using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Features;
using StainlessInfrastructure;
using tld15Server.Composition;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Features.Homes;

public class HomeGetFeature : IFeature
{
    public const string Id = "home.get";
    public static string FeatureId => Id;

    public sealed class Result
    {
        public SharedContent Lore { get; set; } = new();
        public SharedContent Social { get; set; } = new();
        public SharedContent Contacts { get; set; } = new();

        public List<SharedProjectPreview> Projects { get; set; } = [];
        public List<SharedArticlePreview> Articles { get; set; } = [];
    }

    public sealed record Query : IQuery<Result>
    {
        public required string Language { get; set; }
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> dataContextBusiness) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var result = new Result();

            using (var contextBusiness = await dataContextBusiness.CreateDbContextAsync(ctn))
            {
                var contextIds = new[] { Globals.Content.Lore, Globals.Content.Social, Globals.Content.Contacts };

                var content = await contextBusiness
                    .Contents
                    .Where(x => contextIds.Contains(x.Id))
                    .Select(x => new
                    {
                        x.Id,
                        Translations = x.Translations.Select(tr => new
                        {
                            tr.LanguageId,
                            tr.Name,
                            tr.Json,
                            tr.Html,
                        })
                    })
                    .ToListAsync(ctn);


                var lore = content.First(x => x.Id == Globals.Content.Lore);
                result.Lore = new SharedContent
                {
                    Id = lore.Id,
                    Title = lore.Translations.FirstOrDefault(x => x.LanguageId == query.Language).Name,
                    Html = lore.Translations.FirstOrDefault(x => x.LanguageId == query.Language).Html,
                };

                var social = content.First(x => x.Id == Globals.Content.Social);
                result.Social = new SharedContent
                {
                    Id = social.Id,
                    Title = social.Translations.FirstOrDefault(x => x.LanguageId == query.Language).Name,
                    Json = social.Translations.FirstOrDefault(x => x.LanguageId == query.Language).Json,
                };

                var contacts = content.First(x => x.Id == Globals.Content.Contacts);
                result.Contacts = new SharedContent
                {
                    Id = contacts.Id,
                    Title = contacts.Translations.FirstOrDefault(x => x.LanguageId == query.Language).Name,
                    Json = contacts.Translations.FirstOrDefault(x => x.LanguageId == query.Language).Json,
                };


                var projects = await contextBusiness.Projects
                    .Include(x => x.Translations)
                    .Include(x => x.Division).ThenInclude(x => x.Translations)
                    .ToListAsync(ctn);

                result.Projects = projects
                    .Where(x => x.ProjectTypeId == Globals.ProjectType.Project)
                    .Select(x => new SharedProjectPreview
                    {
                        Id = x.Id,
                        DivisionId = x.DivisionId,
                        Title = x.Translations.FirstOrDefault(x => x.LanguageId == query.Language)?.Title,
                        Subtitle = x.Translations.FirstOrDefault(x => x.LanguageId == query.Language)?.Subtitle,
                        DivisionName = x.Division.Translations.FirstOrDefault(x => x.LanguageId == query.Language)?.Name,
                        CreatedAt = x.CreatedAt,
                        PosterAlt = x.Translations.FirstOrDefault(x => x.LanguageId == query.Language)?.PosterAlt,
                        PosterUrl = x.PosterUrl,
                        LinksJson = x.LinksJson
                    })
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();



                result.Articles = projects
                    .Where(x => x.ProjectTypeId == Globals.ProjectType.Project)
                    .Select(x => new SharedArticlePreview
                    {
                        Id = x.Id,
                        DivisionId = x.DivisionId,
                        Title = x.Translations.FirstOrDefault(x => x.LanguageId == query.Language)?.Title,
                        Subtitle = x.Translations.FirstOrDefault(x => x.LanguageId == query.Language)?.Subtitle,
                        DivisionName = x.Division.Translations.FirstOrDefault(x => x.LanguageId == query.Language)?.Name,
                        CreatedAt = x.CreatedAt,
                        PosterAlt = x.Translations.FirstOrDefault(x => x.LanguageId == query.Language)?.PosterAlt,
                        PosterUrl = x.PosterUrl,
                    })
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
            }

            return result;
        }
    }
}
