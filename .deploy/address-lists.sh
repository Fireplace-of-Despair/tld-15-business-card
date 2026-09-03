#!/bin/sh
# tld-15 business card address lists. This script writes the two files that the Caddyfile imports.
#
#   region.caddy  the address ranges of the countries that you name
#   cloud.caddy   the address ranges of a cloud provider, today Amazon Web Services
#
# Both lists stay on the server. Nothing reads a network at request time. No account, no API key and
# no third party sit in the path of a request.
#
# The sources are the delegation files of the five address registries, and the public range file of
# Amazon. Each source is free, and each one needs no login.
#
# Usage:
#   sudo COUNTRIES="CN RU" ./address-lists.sh                    # blacklist: block these countries
#   sudo MODE=allow COUNTRIES="JP US" ./address-lists.sh         # whitelist: allow these countries
#   sudo AWS_SERVICES="EC2" ./address-lists.sh                   # block the compute ranges of Amazon
#   sudo COUNTRIES="" ./address-lists.sh                         # region filter off
#   sudo AWS_SERVICES="" ./address-lists.sh                      # cloud filter off
#   sudo RELOAD=1 COUNTRIES="CN RU" AWS_SERVICES="EC2" ./address-lists.sh
#
# An unset variable leaves its list alone. COUNTRIES="" and AWS_SERVICES="" turn a list off. The
# script therefore writes one list without a touch of the other one.
#
# MODE names the variant that your Caddyfile holds for the region filter. The script writes the same
# file for both variants. MODE only turns on the safety checks of the variant.
#
# Read step 14 of README.md before the first run.

set -eu

MODE="${MODE:-deny}"
LIST_DIR="${LIST_DIR:-/config/caddy/lists}"
CACHE="${CACHE:-/var/cache/tld15-lists}"
MAX_AGE_HOURS="${MAX_AGE_HOURS:-24}"
RELOAD="${RELOAD:-0}"
COMPOSE_DIR="${COMPOSE_DIR:-/config}"

REGION_FILE="$LIST_DIR/region.caddy"
CLOUD_FILE="$LIST_DIR/cloud.caddy"

# RFC 5737 reserves this range for documentation. No client on the internet holds an address from
# it. An empty list writes this range, because the remote_ip matcher of Caddy needs at least one
# argument. The filter then runs and blocks nobody.
PLACEHOLDER="192.0.2.0/24"

REGISTRIES="
https://ftp.ripe.net/pub/stats/ripencc/delegated-ripencc-extended-latest
https://ftp.arin.net/pub/stats/arin/delegated-arin-extended-latest
https://ftp.apnic.net/stats/apnic/delegated-apnic-extended-latest
https://ftp.afrinic.net/stats/afrinic/delegated-afrinic-extended-latest
https://ftp.lacnic.net/pub/stats/lacnic/delegated-lacnic-extended-latest
"
AWS_URL="https://ip-ranges.amazonaws.com/ip-ranges.json"

log() { printf '%s\n' "$*" >&2; }
die() { printf 'error: %s\n' "$*" >&2; exit 1; }

case "$MODE" in
	deny | allow) ;;
	*) die "MODE holds \"$MODE\". Use deny or allow." ;;
esac

case "$LIST_DIR" in
	*" "*) die "LIST_DIR holds a space. The script cannot handle such a path." ;;
esac

command -v awk > /dev/null 2>&1 || die "awk is missing."

# --- What this run writes --------------------------------------------------
# An unset variable leaves its list alone, because a run for one list must never clear the other
# one. A missing file is the exception. Caddy does not start without both files, so the first run
# writes the off state of each list that has no file yet.
WRITE_REGION=0
WRITE_CLOUD=0

if [ -n "${COUNTRIES+x}" ]; then
	WRITE_REGION=1
elif [ ! -f "$REGION_FILE" ]; then
	WRITE_REGION=1
	COUNTRIES=""
	log "bootstrap  $REGION_FILE is missing. The script writes the off state."
fi

if [ -n "${AWS_SERVICES+x}" ]; then
	WRITE_CLOUD=1
elif [ ! -f "$CLOUD_FILE" ]; then
	WRITE_CLOUD=1
	AWS_SERVICES=""
	log "bootstrap  $CLOUD_FILE is missing. The script writes the off state."
fi

if [ "$WRITE_REGION" = "0" ] && [ "$WRITE_CLOUD" = "0" ]; then
	die "nothing to do. Set COUNTRIES, or AWS_SERVICES, or both."
fi

# --- The country list ------------------------------------------------------
# Uppercase the input, and accept an ISO 3166-1 alpha-2 code only. A typo such as "UK" gives no
# address, and a silent empty list turns a whitelist into a full block.
if [ "$WRITE_REGION" = "1" ]; then
	COUNTRIES=$(printf '%s' "$COUNTRIES" | tr 'a-z' 'A-Z' | tr -s ' ,\t' '\n' | sed '/^$/d' | LC_ALL=C sort -u | tr '\n' ' ' | sed 's/ $//')
	for code in $COUNTRIES; do
		case "$code" in
			[A-Z][A-Z]) ;;
			*) die "\"$code\" is not an ISO 3166-1 alpha-2 country code." ;;
		esac
	done
	if [ -z "$COUNTRIES" ]; then
		if [ "$MODE" != "deny" ]; then
			die "MODE=allow needs at least one country. An empty whitelist blocks every client."
		fi
		log "note       COUNTRIES is empty. The region filter blocks nobody."
	fi
fi

# --- The Amazon service list -----------------------------------------------
# Amazon names a service in each entry of its range file. EC2 holds the compute ranges, and a bot
# runs there. AMAZON is the superset, and it also holds CloudFront and S3.
if [ "$WRITE_CLOUD" = "1" ]; then
	AWS_SERVICES=$(printf '%s' "$AWS_SERVICES" | tr 'a-z' 'A-Z' | tr -s ' ,\t' '\n' | sed '/^$/d' | LC_ALL=C sort -u | tr '\n' ' ' | sed 's/ $//')
	for name in $AWS_SERVICES; do
		case "$name" in
			*[!A-Z0-9_]*) die "\"$name\" is not an Amazon service name. Use a name such as EC2." ;;
		esac
	done
	if [ -z "$AWS_SERVICES" ]; then
		log "note       AWS_SERVICES is empty. The cloud filter blocks nobody."
	fi
fi

# A variable that this run does not write stays unset. Read both with a default here.
if [ -n "${COUNTRIES-}${AWS_SERVICES-}" ]; then
	command -v curl > /dev/null 2>&1 || die "curl is missing."
fi

mkdir -p "$LIST_DIR" "$CACHE"

WRITTEN=""
LIST=$(mktemp)
NEW=$(mktemp)
trap 'rm -f "$LIST" "$NEW"' EXIT INT TERM

# --- Helpers ---------------------------------------------------------------

# fetch <url>. The file lands in CACHE under its own name. A cached file that is younger than
# MAX_AGE_HOURS stays. Each source writes its file one time each day.
fetch() {
	_url="$1"
	_file="$CACHE/$(basename "$_url")"
	if [ -f "$_file" ] && [ -z "$(find "$_file" -mmin "+$((MAX_AGE_HOURS * 60))" 2> /dev/null)" ]; then
		log "cached     $(basename "$_file")"
		return 0
	fi
	log "fetch      $_url"
	if curl -fsSL --retry 3 --retry-delay 5 --max-time 300 -o "$_file.tmp" "$_url"; then
		mv "$_file.tmp" "$_file"
		return 0
	fi
	rm -f "$_file.tmp"
	[ -f "$_file" ] || die "the download of $_url failed and no cached copy exists."
	log "warning: the download of $_url failed. The script uses the cached copy."
}

# write_ranges <target file> <source name> <selection>. It reads the ranges from LIST.
# A backslash at the end of a line continues the line. The file therefore holds one range per line,
# and a diff of two runs stays readable.
write_ranges() {
	_target="$1"
	_source="$2"
	_selection="$3"
	_count=$(wc -l < "$LIST" | tr -d ' ')
	[ "$_count" -gt 0 ] || die "the selection \"$_selection\" gave no address range. Check each name."
	{
		printf '# Generated file. Do not edit. address-lists.sh writes it.\n'
		printf '# Source:    %s\n' "$_source"
		printf '# Selection: %s\n' "$_selection"
		printf '# Ranges:    %s\n' "$_count"
		printf '# Written:   %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
		printf 'remote_ip \\\n'
		awk 'NR > 1 { printf "\t%s \\\n", prev } { prev = $0 } END { printf "\t%s\n", prev }' "$LIST"
	} > "$NEW"
	if [ -f "$_target" ]; then
		cp -p "$_target" "$_target.bak"
	fi
	cp "$NEW" "$_target"
	chmod 644 "$_target"
	WRITTEN="$WRITTEN $_target"
	log "wrote      $_target with $_count ranges"
}

# restore. It puts every file of this run back, after a failed check.
restore() {
	for _f in $WRITTEN; do
		if [ -f "$_f.bak" ]; then
			mv "$_f.bak" "$_f"
			log "restored   $_f"
		else
			log "keep       $_f has no earlier copy. Delete it, or write a correct one."
		fi
	done
}

# --- The region list -------------------------------------------------------
# A line of a delegation file holds: registry|cc|type|start|value|date|status|opaque-id
# For ipv4 the value counts addresses, not bits. The awk block below cuts that count into aligned
# CIDR blocks. For ipv6 the value already holds the prefix length.
if [ "$WRITE_REGION" = "1" ]; then
	if [ -n "$COUNTRIES" ]; then
		for url in $REGISTRIES; do
			fetch "$url"
		done
		awk -v want=" $COUNTRIES " '
			function ip2num(s, p) {
				split(s, p, ".")
				return ((p[1] * 256 + p[2]) * 256 + p[3]) * 256 + p[4]
			}
			function num2ip(n, a, b, c, d) {
				a = int(n / 16777216); n = n % 16777216
				b = int(n / 65536);    n = n % 65536
				c = int(n / 256);      d = n % 256
				return a "." b "." c "." d
			}
			BEGIN { FS = "|" }
			$7 != "allocated" && $7 != "assigned" { next }
			index(want, " " $2 " ") == 0 { next }
			$3 == "ipv4" {
				n = ip2num($4)
				c = $5 + 0
				while (c > 0) {
					size = 1
					bits = 32
					while (size * 2 <= c && n % (size * 2) == 0) { size = size * 2; bits = bits - 1 }
					print num2ip(n) "/" bits
					n = n + size
					c = c - size
				}
				next
			}
			$3 == "ipv6" { print $4 "/" $5 }
		' "$CACHE"/delegated-*-extended-latest | LC_ALL=C sort -u > "$LIST"
		write_ranges "$REGION_FILE" "the five address registries" "$COUNTRIES, mode $MODE"
	else
		printf '%s\n' "$PLACEHOLDER" > "$LIST"
		write_ranges "$REGION_FILE" "none" "off, the placeholder range only"
	fi
fi

# --- The cloud list --------------------------------------------------------
# Amazon writes one object for each range. Each object holds ip_prefix or ipv6_prefix, and it names
# its service. The record separator below cuts the file into those objects.
if [ "$WRITE_CLOUD" = "1" ]; then
	if [ -n "$AWS_SERVICES" ]; then
		fetch "$AWS_URL"
		awk -v want=" $AWS_SERVICES " '
			function field(rec, key, v) {
				if (match(rec, "\"" key "\"[ \t]*:[ \t]*\"[^\"]+\"") == 0) {
					return ""
				}
				v = substr(rec, RSTART, RLENGTH)
				sub("^\"" key "\"[ \t]*:[ \t]*\"", "", v)
				sub("\"$", "", v)
				return v
			}
			BEGIN { RS = "}" }
			{
				service = field($0, "service")
				if (service == "" || index(want, " " service " ") == 0) {
					next
				}
				prefix = field($0, "ip_prefix")
				if (prefix == "") {
					prefix = field($0, "ipv6_prefix")
				}
				if (prefix != "") {
					print prefix
				}
			}
		' "$CACHE/$(basename "$AWS_URL")" | LC_ALL=C sort -u > "$LIST"
		write_ranges "$CLOUD_FILE" "Amazon Web Services, $AWS_URL" "$AWS_SERVICES"
	else
		printf '%s\n' "$PLACEHOLDER" > "$LIST"
		write_ranges "$CLOUD_FILE" "none" "off, the placeholder range only"
	fi
fi

# --- Validate --------------------------------------------------------------
# Caddy reads both files as a part of the Caddyfile. A broken file stops Caddy at the next start,
# and a container that does not start serves nobody. Validate now, and put the earlier files back
# when the check fails.
#
# Two commands run the check. A running container answers "docker compose exec". Before the first
# start of the stack no container runs, and exec then fails with "service caddy is not running".
# The script uses "docker compose run" in that case. That command starts a throw-away container,
# mounts the same two folders, and removes the container after the check.
COMPOSE=0
if command -v docker > /dev/null 2>&1 && [ -f "$COMPOSE_DIR/docker-compose.yml" ]; then
	COMPOSE=1
fi

if [ "$COMPOSE" = "0" ]; then
	log "skip       no docker-compose.yml in $COMPOSE_DIR, so the script checks nothing. Then run:"
	log "           cd $COMPOSE_DIR && sudo docker compose run --rm --no-deps caddy caddy validate --config /etc/caddy/Caddyfile"
	exit 0
fi

CADDY_ID=$(cd "$COMPOSE_DIR" && docker compose ps -q caddy 2> /dev/null || true)

# The word splitting below is deliberate. Each variant holds more than one argument.
if [ -n "$CADDY_ID" ]; then
	CADDY_RUN="exec -T caddy"
else
	CADDY_RUN="run --rm --no-deps -T caddy"
	log "note       Caddy does not run. The check starts a throw-away container instead."
fi

# shellcheck disable=SC2086
if ! (cd "$COMPOSE_DIR" && docker compose $CADDY_RUN caddy validate --config /etc/caddy/Caddyfile > /dev/null 2>&1); then
	restore
	die "Caddy rejected the new list. A missing caddy image gives the same answer before the first start."
fi
log "valid      the Caddyfile passes the check"

# A reload needs a live process. A throw-away container holds none, so this step waits for the stack.
if [ -z "$CADDY_ID" ]; then
	log "next       start the stack, then run:"
	log "           cd $COMPOSE_DIR && sudo docker compose up -d"
elif [ "$RELOAD" = "1" ]; then
	(cd "$COMPOSE_DIR" && docker compose exec -T caddy caddy reload --config /etc/caddy/Caddyfile)
	log "reload     Caddy reads the new lists"
else
	log "next       cd $COMPOSE_DIR && sudo docker compose exec caddy caddy reload --config /etc/caddy/Caddyfile"
fi
