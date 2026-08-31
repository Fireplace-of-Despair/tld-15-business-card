# Deploy Stainless Tasks

This folder holds the four files that a server needs. The steps below take a new virtual server to a
running site behind HTTPS. The steps assume no earlier setup. Do them in order. Plan about 40 minutes.

| File | Goes to | Purpose |
|---|---|---|
| `docker-compose.yml` | `/config/docker-compose.yml` | Two containers: the server and the reverse proxy. |
| `Caddyfile.example` | `/config/caddy/Caddyfile` | The reverse proxy, the certificate, and the security rules. |
| `address-lists.sh` | `/config/caddy/address-lists.sh` | Writes the address lists of the two address filters. |
| `appsettings.example.json` | `/config/tld15/appsettings.json` | The settings of the server. |

The server keeps one folder for each part. `/config` holds the Compose file. `/config/caddy` holds
the reverse proxy. `/config/tld15` holds the server. Step 10 makes these folders.

```
/config
├── docker-compose.yml
├── caddy
│   ├── Caddyfile
│   ├── address-lists.sh
│   ├── lists
│   └── logs
└── tld15
    ├── appsettings.json
    └── logs
```

**The Compose file holds no database.** Run PostgreSQL where you want it. It can run on the same
machine, on another server, or with a managed provider. Step 8 gives both common cases.

---

## Build the image

The server publishes as one container image. The `Dockerfile` sits in `tld15Server/`. Each `COPY` line
reads from the repository root. Build from the root, and give the file to the `-f` option.

```bash
docker build -f tld15Server/Dockerfile -t tld15-server:latest .
```

The `Dockerfile` holds two build stages. The restore stage copies one `.csproj` per project. The
publish stage copies the source folder of each of those projects. A new project reference needs a line
in both stages. A missing line in the publish stage lets the build reach the compiler and fail there.

The build runs on Alpine and targets `linux-musl-x64`. The image stays framework-dependent. The
runtime stage uses the `-extra` tag, which carries ICU and the time zone database. The server needs
both, because it serves Japanese and it reads the time zone of the browser. Never move to the plain
tag. It ships no ICU, and Japanese then falls back to the invariant culture. The clock of the
container reads UTC.

The publish stage drops the XML documentation files and the JavaScript source maps. An editor and a
browser debugger read those. The server reads neither one.

The image measures about 247 MB on disk and about 71 MB as a gzip file. Two thirds of that is the
.NET runtime and ICU. Neither one can shrink without a loss.

`.dockerignore` names `**/appsettings.json` and `**/appsettings.Development.json`. Neither file enters
the image. Your pepper and your connection string stay on your machine.

The image holds a `HEALTHCHECK`. It calls `/api/public/ping` each 30 seconds. It allows 40 seconds for
the migration run at start-up. That endpoint answers `pong` with a live database. It answers `wrong`
without one. Both answers carry HTTP 200, so the check reads the body.

```bash
docker inspect --format '{{.State.Health.Status}}' tld15-server
```

Write the image to a file for the server.

```bash
docker save tld15-server:latest | gzip > tld15-server-1.0.0.tar.gz
```

Windows:
```bash 
docker save -o tld15-server-1.0.0.tar tld15-server-1.0.0:latest
```

---

## Deploy to a server

**What you need**

- A virtual server with Debian 12 or later, 2 GB of memory, and 20 GB of disk.
- A domain name that you control.
- An SSH key pair on your own computer. Make one with `ssh-keygen -t ed25519` when you have none.
- A PostgreSQL 15 or later database. Step 8 also shows how to install one.

### 1. Point the domain at the server

Add an `A` record for the IPv4 address of the server. Add an `AAAA` record when the server has an IPv6
address. Wait until the record answers.

```bash
dig +short tasks.example.com
```

The certificate authority reads this record. A wrong record stops the certificate.

### 2. Sign in and update

```bash
ssh root@tasks.example.com
```

```bash
apt update && apt full-upgrade -y && reboot
```

### 3. Make a user that is not root

```bash
adduser tld
```

```bash
usermod -aG sudo tld
```

Copy your public key to the new user. Run this command on your own computer, not on the server.

```bash
ssh-copy-id tld@tasks.example.com
```

### 4. Restrict SSH

Write the settings into a drop-in file.
edit: nano /etc/ssh/sshd_config
```bash
PermitRootLogin no
PasswordAuthentication no
KbdInteractiveAuthentication no
PubkeyAuthentication yes
MaxAuthTries 3
X11Forwarding no
AllowUsers tld
```

Check the file. Then restart the service.

```bash
sudo sshd -t && sudo systemctl restart ssh
```

> **Keep the current session open.** Open a second terminal, and sign in as `tld`. Close the first
> terminal only after the second one works. An error in this file stops every SSH sign-in. Only the
> console of your hosting provider gives access again.

Debian 12 and later read `/etc/ssh/sshd_config.d/*.conf`. On an older Debian, put the same lines at the
end of `/etc/ssh/sshd_config`.

### 5. Enable automatic security updates

```bash
sudo apt install -y unattended-upgrades apt-listchanges
```

```bash
sudo dpkg-reconfigure -plow unattended-upgrades
```

### 6. The firewall

```bash
sudo apt install -y ufw
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp
sudo ufw enable
```

> **ufw does not protect a container port.** Docker writes its own NAT rules. A published port reaches
> the container before ufw reads the packet. Ports 80 and 443 therefore stay open, and a ufw rule does
> not change that. For this reason `docker-compose.yml` publishes those two ports only. The server
> publishes no port, so it answers on the internal network alone. Step 9 blocks an address at the
> Docker chain, where a block does work.

> **Your hosting provider holds a second firewall.** It sits ahead of the machine, and `ufw status`
> does not show it. Open port 80 and port 443 for every address in the panel of the provider.
>
> Port 443 carries the site. Port 80 carries the certificate challenge, and the certificate authority
> reads it from the internet. A closed port 80 gives no certificate, so the site never answers on
> port 443 either. Step 16 holds the check.

### 7. Docker

Install from the repository of Docker, not from the repository of Debian.

```bash
sudo apt install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
```

```bash
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
```

```bash
sudo apt update && sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

> Do not add your user to the `docker` group. That group gives root rights over the full machine,
> because a container can mount the disk of the host. Run each Docker command with `sudo`.

### 8. The database

The server needs one PostgreSQL database and one role. The server makes each table at start-up.

```sql
CREATE ROLE tld15 WITH LOGIN PASSWORD 'a-long-random-password';
CREATE DATABASE tld15 OWNER tld15;
```

Make the password with `openssl rand -base64 36`.

**A database on another server or with a provider.** Write its address into the connection string in
step 12. Nothing else changes. Delete the `extra_hosts` block from `docker-compose.yml`.

**A database on the same machine.** Install it. Then open it to the container network only.

```bash
sudo apt install -y postgresql
```

The container reaches the host at `host.docker.internal`. `docker-compose.yml` maps that name to the
gateway of the Compose network. PostgreSQL must listen on that address. It must also accept the
container network.

```bash
# /etc/postgresql/*/main/postgresql.conf
listen_addresses = 'localhost,172.28.0.1'
```

```bash
# /etc/postgresql/*/main/pg_hba.conf
host    tld15    tld15    172.28.0.0/16    scram-sha-256
```

```bash
sudo systemctl restart postgresql
sudo ufw allow from 172.28.0.0/16 to any port 5432 proto tcp
```

> Never publish port 5432 to the internet. The rule above allows the Compose network only.

### 9. fail2ban

fail2ban reads a log. It finds a repeated failure from one address. It then blocks the address.

```bash
sudo apt install -y fail2ban
```

The default action of fail2ban writes into the `INPUT` chain. A container port passes that chain. The
action below writes into `DOCKER-USER`. Each container port passes through that chain.

```bash
sudo tee /etc/fail2ban/action.d/docker-user.conf > /dev/null <<'EOF'
[Definition]
actionstart = iptables -N f2b-docker 2>/dev/null || true
              iptables -A f2b-docker -j RETURN
              iptables -I DOCKER-USER -j f2b-docker
actionstop  = iptables -D DOCKER-USER -j f2b-docker
              iptables -F f2b-docker
              iptables -X f2b-docker
actioncheck = iptables -n -L DOCKER-USER | grep -q 'f2b-docker'
actionban   = iptables -I f2b-docker 1 -s <ip> -j DROP
actionunban = iptables -D f2b-docker -s <ip> -j DROP
EOF
```

Two filters read the JSON access log of Caddy. They differ in how sure the status code is.

**Each filter reads the address of the caller, not the address of the proxy.** Caddy writes
`remote_ip` for the machine that opened the connection. Behind a content delivery network that
machine is the network, and not the client. A filter that reads `remote_ip` then bans the network,
and the site loses every visitor at the same moment. Cloudflare gives the address of the client in
the `Cf-Connecting-Ip` header, and Caddy writes every request header into the access log.

Each filter below therefore holds two lines. The first line reads `Cf-Connecting-Ip`. The second
line runs only when the log line holds no such header, which is the case for a Caddy at the edge.
One log line gives one address, and never two.

> **Allow port 80 and port 443 from the network only.** A client writes the `Cf-Connecting-Ip`
> header itself. A caller that reaches the server directly can therefore name any address, and
> fail2ban then bans that address. An attacker uses this to lock out a chosen user. Allow the
> published ranges of your network in the firewall of step 6, and drop every other source.

**The scanner filter.** Status `0` marks a request that the proxy dropped. A scan for `/wp-login.php`
gives that status. A call that names `sqlmap` in its `User-Agent` also gives that status. A correct
client never receives one of these codes, so this filter bans hard.

```bash
sudo tee /etc/fail2ban/filter.d/caddy-scanner.conf > /dev/null <<'EOF'
[Definition]
failregex = ^.*"Cf-Connecting-Ip":\["<HOST>"\].*"status":(?:0|400|403|405|413)[,}]
            ^(?!.*"Cf-Connecting-Ip").*"remote_ip":"<HOST>".*"status":(?:0|400|403|405|413)[,}]
ignoreregex =
datepattern = "ts":"%%Y-%%m-%%dT%%H:%%M:%%S
EOF
```

**The client-error filter.** Status `401` and status `404` also come from a correct client. A client
with an old API key receives `401` from `ApiKeyFilter`. A browser that follows a dead link receives
`404`. This filter therefore bans late, and it bans for one hour, not for one day.

```bash
sudo tee /etc/fail2ban/filter.d/caddy-client-error.conf > /dev/null <<'EOF'
[Definition]
failregex = ^.*"Cf-Connecting-Ip":\["<HOST>"\].*"status":(?:401|404)[,}]
            ^(?!.*"Cf-Connecting-Ip").*"remote_ip":"<HOST>".*"status":(?:401|404)[,}]
ignoreregex =
datepattern = "ts":"%%Y-%%m-%%dT%%H:%%M:%%S
EOF
```

> **Neither filter reads status `429`.** The server answers `429` through `Security:RateLimit` in step
> 12. That answer already slows the caller down. A ban on the same code punishes one caller twice, and
> it blocks every user behind the same address.

```bash
sudo tee /etc/fail2ban/jail.local > /dev/null <<'EOF'
[DEFAULT]
bantime  = 1h
findtime = 10m
maxretry = 5

[sshd]
enabled = true
backend = systemd

[caddy-scanner]
enabled  = true
filter   = caddy-scanner
logpath  = /config/caddy/logs/access.log
backend  = polling
maxretry = 20
findtime = 10m
bantime  = 24h
action   = docker-user

[caddy-client-error]
enabled  = true
filter   = caddy-client-error
logpath  = /config/caddy/logs/access.log
backend  = polling
maxretry = 60
findtime = 10m
bantime  = 1h
action   = docker-user
EOF
```

Start fail2ban after the stack runs. Each jail needs the log file. Step 17 starts fail2ban.

### 10. The folders

```bash
sudo mkdir -p /config/tld15/logs /config/caddy/logs /config/caddy/lists
```

`/config/caddy/lists` holds the address lists of the region filter and of the cloud filter. Step 14
writes the two files inside it. Caddy reads that folder as read-only, so the folder keeps the owner
`root`.

Each container writes its log as a different user. Give each log folder the owner of its container.

```bash
sudo chown -R 1654:1654 /config/tld15/logs
sudo chown -R root:root /config/caddy/logs
sudo chmod 755 /config/tld15/logs /config/caddy/logs
```

> **The owner of a log folder must match the user of its container.** The server runs as user `1654`
> inside its image. Caddy runs as `root` inside its image. A folder with the other owner gives no log.
>
> `docker-compose.yml` sets `cap_drop: ALL` for both containers. `root` inside Caddy therefore loses
> `CAP_DAC_OVERRIDE`. That capability lets `root` ignore the permission bits of a file. Without it
> Caddy obeys the owner and the mode of the folder, the same as every other user. A folder that
> belongs to `1654` then stops the start:
>
> ```
> open /var/log/caddy/access.log: permission denied
> ```
>
> Never run `chown -R` over `/config`. One command then gives one owner to both log folders, and one
> of the two containers fails. Read the owner with `ls -ln /config/caddy /config/tld15`.

### 11. The files

Copy the four files from this folder to the server. The table at the top of this page names the place
of each one. Then set the rights. One file holds the pepper and the database password.

```bash
cd /config/tld15
sudo chown 1654:1654 appsettings.json
sudo chmod 600 appsettings.json
```

```bash
sudo chmod 755 /config/caddy/address-lists.sh
```

Close the folder itself as well. `chmod 600` protects the file. It does not stop another user from
reading the names of the files next to it.

```bash
sudo chmod 750 /config/tld15
```

### 12. Edit appsettings.json

`appsettings.example.json` starts from `tld15Server/appsettings.Development.json`. It already holds the
production values. Replace each `CHANGE_ME` and each `tasks.example.com`.

| Key | Development value | Set it to |
|---|---|---|
| `ConnectionStrings:PostgreSQL` | `Server=localhost;...;UserId=postgres;Password=sa` | the address, the role and the password from step 8 |
| `Security:Pepper` | `qwerty` | 40 or more random characters |
| `Security:ForwardedHeaders:KnownNetworks` | `[]` | `[ "172.28.0.0/16" ]` |
| `Security:RateLimit:GlobalPermits` | `1000` | `300`, or a value that fits your users |
| `Security:RateLimit:ApiPermits` | `100` | `30`, or a value that fits your clients |
| `Application:SourceUrl` | this repository | your fork, when you run modified code |
| `Serilog:MinimumLevel` | `Debug` | `Information` |
| `Serilog:WriteTo` | console only | console and a file at `/app/logs/tld15-.log` |
| `AllowedHosts` | `*` | `tasks.example.com` |

Keep the other keys. `SaltSize`, `CookieExpiration`, `CookieMaxAge` and the three `Login*` keys all
hold a correct value.

Two keys of the example file look like settings and change nothing today. No code reads
`Application:Host`. No code reads `Automation:TimeoutMinutes`. The site address comes from the `Host`
header that Caddy passes through. `AllowedHosts` is the key that checks that header.

> **A large push meets three limits.** `request_body` in the Caddyfile allows 40 MB. Kestrel allows
> 30 MB of its own, and the server sets no other value. The lower size limit therefore comes from
> Kestrel. `read_body` in the Caddyfile adds a time limit of 2 minutes for the full body. A snapshot
> of five thousand issues stays near 8 MB and needs 68 KB each second, so all three limits have a
> margin.

> **Set the pepper one time, before the first sign-in.** The server mixes the pepper into each password
> hash and into each API-key hash. A later change stops every account and every API key. Only a new
> database recovers from that change.

> **`KnownNetworks` must match the subnet of the Compose network.** The server reads the caller address
> from `X-Forwarded-For` only when Caddy sends it from that range. An empty list makes each user look
> like Caddy. One wrong password then locks every user. A different `subnet:` in `docker-compose.yml`
> needs the same value here.

> **`Security:RateLimit` counts each caller address over a fixed window.** `GlobalPermits` covers every
> request. `ApiPermits` adds a second, lower limit on `/api/protected`, which the two clients use to
> sync. A caller over a limit receives HTTP 429 and a `Retry-After` header. A permit value of `0` turns
> that limiter off.
>
> The limiter partitions on the same address that `KnownNetworks` produces. A wrong `KnownNetworks`
> therefore puts every user into one partition, and one busy client then rejects all the others. Set
> both keys, or set neither.
>
> Start high and lower the value later. One cold page load asks for the static files as well, so a
> browser sends tens of requests in a few seconds. Watch the log for `429` before you tighten this.

### 13. Edit the Caddyfile

Replace `tasks.example.com` with your domain. Replace `admin@example.com` with your address. Read the
comments in the file before you change a rule.

The file drops a request in six cases:

1. The address of the client belongs to a blocked country. Step 14 holds this rule.
2. The address belongs to a blocked cloud provider. Step 14 holds this rule as well.
3. The path matches a known scanner path.
4. The file name ending is one that this application never serves.
5. The `User-Agent` names a scanner, a script library, or an AI harvester.
6. The `User-Agent` header is absent or empty.

> **Case 6 needs a current client.** The desktop client and the iOS client send
> `tld15ClientDesktop/<version>` and `tld15ClientMobile/<version>`.
> As an alternative, delete the `@no_agent` line and the
> `@empty_agent` line from the `route` block.

### 14. The address filters

Caddy drops a request by the country of its address, and by the cloud provider that holds the
address. Both rules run on this machine. No request reaches a third party, and no answer waits for
one.

`address-lists.sh` writes two files into `/config/caddy/lists/`, and the Caddyfile imports both.

| File | Holds | Source |
|---|---|---|
| `region.caddy` | the ranges of the countries that you name | the delegation files of RIPE NCC, ARIN, APNIC, AFRINIC and LACNIC |
| `cloud.caddy` | the ranges of the cloud services that you name | the public range file of Amazon Web Services |

Each source is free, and each one needs no login. The script needs `curl` and `awk`. Debian ships
both.

**One variable, one list.** An unset variable leaves its list alone. A run with `COUNTRIES` never
touches `cloud.caddy`, and a run with `AWS_SERVICES` never touches `region.caddy`. The first run
writes the off state for each file that does not exist yet.

> **Caddy does not start without those two files.** Run the script one time before step 16. This
> behavior is deliberate. A missing file must never turn a filter off in silence.

#### The region filter

Pick one of the two variants. The Caddyfile holds both inside the `filter_matchers` snippet, and
it names them `Variant 1` and `Variant 2`. Each site block that imports the snippet takes the
variant that you keep.

| Variant | Blocks | Pick it when |
|---|---|---|
| 1. Blacklist, the default | the countries that you name | you want to cut the noise of a scan |
| 2. Whitelist | every country that you do not name | you know the country of every user |

**The blacklist.** Name the countries that you block. Every other country reaches the site. Use an ISO
3166-1 alpha-2 code.

```bash
sudo COUNTRIES="CN RU" /config/caddy/address-lists.sh
```

An empty country list writes one range from RFC 5737. No client on the internet holds an address from
that range, so the filter runs and blocks nobody. This is the off state, and it lets Caddy start
before you make your choice.

```bash
sudo COUNTRIES="" /config/caddy/address-lists.sh
```

**The whitelist.** Name the countries that you allow. Edit the Caddyfile first.

1. Delete the four lines of the `Variant 1` block.
2. Remove the comment marks from the `Variant 2` block.
3. Write the list with `MODE=allow`.

```bash
cd /config/caddy
sudo MODE=allow COUNTRIES="JP US" ./address-lists.sh
```

`MODE=allow` refuses an empty country list. An empty whitelist blocks every client, including you.

> **A whitelist blocks your own users.** It blocks a user on a trip, a mobile carrier that routes
> through another country, a VPN, and a business partner. It also blocks you. Keep a second way into the server, such
> as SSH plus an SSH tunnel to port 8080. Test the list from a phone on mobile data before you trust
> it.

**What the list knows and what it does not.** A registry records the country of the organization that
holds an address block. That country is not the location of the person at the keyboard. A cloud
address carries the country of the region, not of the customer. A VPN, a proxy and a satellite link
each move a caller to another country. A mobile carrier often routes a whole country through two or
three blocks. Treat the filter as a way to cut noise. Never treat it as an access rule.

#### The cloud filter

A person browses from a home network or from a mobile network. A scanner, a scraper and a flood tool
run on a rented machine. This filter blocks the ranges of Amazon Web Services. It is off until you
name a service.

```bash
sudo AWS_SERVICES="EC2" /config/caddy/address-lists.sh
```

| Service | Ranges | Holds |
|---|---|---|
| `EC2` | about 4,150 | the compute ranges, where a bot runs |
| `AMAZON` | about 8,980 | the superset, which also holds CloudFront and S3 |

Start with `EC2`. `AMAZON` also blocks a visitor who arrives through CloudFront, and a client that
reads an image from S3. Name more than one service with a space between the names.

An empty value turns the filter off again.

```bash
sudo AWS_SERVICES="" /config/caddy/address-lists.sh
```

> **The cloud filter always denies.** It does not invert with the region variant. A whitelist that
> names your country still blocks a rented machine inside that country. A cloud range inside a
> whitelist would let every bot of that provider in.

> **Check three points before you turn it on.**
> 1. Your own users must not sit there. A corporate VPN, a cloud desktop and a few mobile carriers
>    each leave through such a range.
> 2. The path of the certificate challenge stays open for every address. Let's Encrypt validates a
>    domain from several networks, and a cloud provider hosts some of them. Keep the first line of
>    the `@cloud_denied` block.
> 3. An integration that calls your API from a cloud function meets this rule as well.

#### Keep the lists fresh

The registries write their files one time each day. Amazon changes its file a few times each week.
One run each week is enough for both. `RELOAD=1` makes Caddy read the new lists without a restart
and without a dropped connection.

```bash
sudo tee /etc/cron.d/tld15-lists > /dev/null <<'EOF'
17 4 * * 0 root COUNTRIES="CN RU" AWS_SERVICES="EC2" RELOAD=1 /config/caddy/address-lists.sh >> /var/log/tld15-lists.log 2>&1
EOF
```

Use the same variables in the cron job that you used by hand. A cron job with an empty `COUNTRIES`
turns the region filter off. A cron job that names `COUNTRIES` alone leaves `cloud.caddy` alone. A
cron job without `MODE=allow` still writes the correct file for a whitelist, and it loses the safety
check only.

#### Check the result

The script validates the Caddyfile after each write. It puts the earlier files back when the check
fails, and it keeps one copy of each file with the ending `.bak`.

The check runs inside a container. A running Caddy answers `docker compose exec`. The first run of
the script comes before the first start of the stack, so no container runs yet. The script then
starts a throw-away container with `docker compose run`. It removes that container after the check.

Read the two files after a run.

```bash
head -5 /config/caddy/lists/region.caddy /config/caddy/lists/cloud.caddy
```

Check the Caddyfile by hand with one of the two commands below. Use the first one while the stack
runs. Use the second one before the first start.

```bash
cd /config && sudo docker compose exec caddy caddy validate --config /etc/caddy/Caddyfile
```

```bash
cd /config && sudo docker compose run --rm --no-deps caddy caddy validate --config /etc/caddy/Caddyfile
```

> **`exec` needs a running container.** It answers `service "caddy" is not running` before the first
> start, and also after a crash. `run` starts its own container, so it answers in both cases.
>
> The word `caddy` appears two times in the `run` command. The first one names the service. The
> second one names the program. The image of Caddy holds the full command in `CMD` and holds no
> `ENTRYPOINT`, so the arguments must repeat the name of the program.
>
> `--no-deps` keeps the server container out of the check. `run` publishes no port, so this command
> does not disturb a running stack.

Read the state of the containers when `exec` reports that Caddy does not run.

```bash
cd /config && sudo docker compose ps -a
```

```bash
cd /config && sudo docker compose logs --tail 50 caddy
```

> **Both filters feed fail2ban.** A dropped request writes status `0` into the access log. The
> `caddy-scanner` jail from step 9 reads that status. A caller from a blocked country or from a
> blocked cloud range therefore lands at the firewall after 20 requests. The next packet never
> reaches Caddy. A blocked user meets the same jail. `sudo fail2ban-client set caddy-scanner
> unbanip <ip>` releases the address.

#### A large list belongs at the firewall

`remote_ip` names the machine that opened the connection. Behind a content delivery network that
machine is the network, so the two filters then read the country and the range of the network. Set
`trusted_proxies` in the global options and change both matchers to `client_ip` when you run behind
such a network. The filter of fail2ban in step 9 needs the same change, and it already holds it.

The `remote_ip` matcher compares the address against the ranges in order, until one range matches.
Japan gives about 5,500 ranges. China, Japan and Russia together give about 30,000. `EC2` gives
about 4,150. That work stays small next to the TLS handshake of the same request. A much larger
list belongs in an `nftables` set instead, which gives one lookup for any size. Build the set from
the same generated file, and drop the address in the `DOCKER-USER` chain of step 9.

### 15. Load the image

No registry holds the image today. The repository holds it as a file. Download the file, copy it to the
server, and load it.

```bash
scp tld15-server-1.0.0.tar.gz tld@tasks.example.com:/tmp/
```

```bash
sudo docker load -i /tmp/tld15-server-1.0.0.tar.gz
```

`docker load` also brings the tag `tld15-server:latest`. `docker-compose.yml` holds `pull_policy:
never`, so Compose never searches for that name in a registry.

### 16. Start

```bash
cd /config && sudo docker compose up -d
```

The server starts. It applies the migrations. It then reports healthy. Caddy gets the certificate.

```bash
sudo docker compose ps
```

```bash
sudo docker compose logs -f server
```

The server is ready when `docker compose ps` shows `healthy` for `tld15-server`. Caddy needs about 30
seconds for the first certificate.

Read the two published ports. Both must name `0.0.0.0`.

```bash
sudo ss -lntp | grep -E ':80 |:443 '
```

Now read port 80 from the internet. Run this command on your own computer, not on the server.

```bash
curl -sv --max-time 10 http://tasks.example.com/.well-known/acme-challenge/test
```

An answer of `404` is the correct result. It proves that a packet from the internet reaches Caddy.

> **A timeout at this check stops the certificate.** The certificate authority reads the same path.
> It reports `Timeout during connect (likely firewall problem)` in the log of Caddy, and it gives no
> certificate. Read these four points in order:
>
> 1. The `A` record of the domain. It must hold the address of this server. Read it with
>    `dig +short tasks.example.com`.
> 2. The firewall of your hosting provider. Step 6 holds this point.
> 3. The two published ports of the command above.
> 4. The bans of fail2ban. `sudo iptables -n -L DOCKER-USER` shows a `DROP` rule for each one. A
>    `DROP` rule gives a timeout, not a refusal.
>
> Read the answer of the certificate authority in the log.
>
> ```bash
> cd /config && sudo docker compose logs caddy | grep -i acme
> ```

> **The server container runs on a read-only image.** `docker-compose.yml` sets `read_only: true` for
> the server. It gives the application a writable `/app/logs`, which is the bind mount from step 10,
> and a writable `/tmp`. When the container stops at start-up, read the log. A line that names a path
> and `Read-only file system` tells you which path still needs a write. Add a `tmpfs` entry for that
> path.

> **`mem_limit` is a ceiling, not a reservation.** The two values add up to 1.25 GB. On a 2 GB machine
> that also runs PostgreSQL from step 8, lower `mem_limit` of the server to `768m`. A container over
> its limit meets the OOM killer, and a limit that is too high moves that event to PostgreSQL instead.

### 17. The first sign-in

Open `https://tasks.example.com`. The first sign-in makes the administrator account. Open the account
page, and make an API key. Copy the key immediately. The server shows it one time only.

Now start fail2ban. The access log exists from this point.

```bash
sudo systemctl restart fail2ban && sudo fail2ban-client status
```

---

## Update to a new version

The self-hosted flow needs no registry. It also needs no build on the server.

```bash
# 1. On your own computer. Download the new image file from the repository, then copy it to the server.
scp tld15-server-1.1.0.tar.gz tld@tasks.example.com:/tmp/
```

```bash
# 2. On the server. Load it. The tag tld15-server:latest now points at the new image.
sudo docker load -i /tmp/tld15-server-1.1.0.tar.gz
```

```bash
# 3. Replace the container. Compose reads the new image and recreates the server only.
cd /config && sudo docker compose up -d
```

```bash
# 4. Delete the old layers and the file.
sudo docker image prune -f && rm /tmp/tld15-server-1.1.0.tar.gz
```

The migrations run at start-up. Read the log when the container does not reach `healthy`.

```bash
sudo docker compose logs --tail 100 server
```

> **Make a backup of the database before an update that holds a migration.** A migration runs in one
> direction only.

## Update the reverse proxy

The steps above replace the server only. Caddy needs its own update, and nothing on the server does it
for you. `restart: unless-stopped` restarts the container. It never reads a new image.

Caddy terminates TLS for the whole site. An old Caddy carries an old Go runtime and an old TLS stack.
Do this each month.

```bash
cd /config && sudo docker compose pull caddy && sudo docker compose up -d caddy
```

Caddy keeps its certificate in the `caddy-data` volume, so an update asks the certificate authority
for nothing.

`unattended-upgrades` from step 5 does not cover this. That service updates the packages of the host.
It never looks inside a container.

## Update the runtime of the server

The image builds on `mcr.microsoft.com/dotnet/aspnet:11.0-preview-alpine-extra`. A .NET security fix
reaches your server only after you build the image again. Docker keeps the earlier layers, so add
`--pull` to read the new base image.

```bash
docker build --pull -f tld15Server/Dockerfile -t tld15-server:latest .
```

Then follow the four steps of the update above.

> **`11.0-preview` is a preview tag.** Move the `FROM` lines of `tld15Server/Dockerfile` to the stable
> tag as soon as .NET 11 ships. A preview image stops receiving fixes when the stable image arrives.

## Make a backup

The database runs outside the stack. Use `pg_dump` against its address.

```bash
pg_dump -h 172.28.0.1 -U tld15 -d tld15 | gzip > tld15-$(date -u +%Y%m%d).sql.gz
```

Copy the dump to another machine. A dump on the same disk does not survive a disk failure.

## Read the logs

| What | Where |
|---|---|
| The server | `/config/tld15/logs/tld15-<date>.log`, or `sudo docker compose logs server` |
| The reverse proxy | `/config/caddy/logs/access.log` |
| The blocked addresses | `sudo fail2ban-client status caddy-scanner`, and `caddy-client-error` |
| The address filters | `head -5 /config/caddy/lists/*.caddy`, and `/var/log/tld15-lists.log` |
