using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.Managed.Library;
using Light.Managed.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Core
{
    public class PlaybackHistoryManager
    {
        static public int HistoryEntryLimit
        {
            get
            {
                return SettingsManager.Instance.GetValue<int>();
            }
            set
            {
                SettingsManager.Instance.SetValue(value);
            }
        }

        static public bool EnablePlaybackHistory
        {
            get
            {
                return SettingsManager.Instance.GetValue<bool>();
            }
            set
            {
                SettingsManager.Instance.SetValue(value);
            }
        }

        static public PlaybackHistoryManager Instance { get; } = new PlaybackHistoryManager();
        private PlaybackHistoryManager()
        {
            context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>();
        }

        private List<MusicPlaybackItem> _historyCache;

        private IServiceScope scope = ApplicationServiceBase.App.GetScope();

        private MedialibraryDbContext context;

        public event EventHandler<MusicPlaybackItem> NewEntryAdded;

        public List<MusicPlaybackItem> GetHistory(int count)
        {
            if (!EnablePlaybackHistory)
            {
                return new List<MusicPlaybackItem>();
            }

            if (_historyCache == null)
            {
                var query = context.PlaybackHistory
                    .Include(item => item.RelatedMediaFile)
                    .OrderByDescending(item => item.PlaybackTime);

                _historyCache = (count > 0 ? query.Take(count) : query)
                    .Where(x => x.RelatedMediaFile != null)
                    .Select(x => MusicPlaybackItem.CreateFromMediaFile(x.RelatedMediaFile))
                    .ToList();
            }

            if (_historyCache.Count < Math.Min(
                count == 0 ? int.MaxValue : count,
                context.PlaybackHistory.Count()))
            {
                _historyCache.AddRange(context.PlaybackHistory
                    .Include(item => item.RelatedMediaFile)
                    .OrderByDescending(item => item.PlaybackTime)
                    .Skip(_historyCache.Count)
                    .Where(x => x.RelatedMediaFile != null)
                    .Select(x => MusicPlaybackItem.CreateFromMediaFile(x.RelatedMediaFile)));
            }


            return (count > 0 && _historyCache.Count > count ? _historyCache.Take(count) : _historyCache).ToList();
        }

        public async Task ClearHistoryAsync()
        {
            _historyCache?.Clear();
            context.PlaybackHistory.RemoveRange(context.PlaybackHistory);
            await context.SaveChangesAsync();
        }

        public async Task ClearHistoryAsync(int limit)
        {
            if (!EnablePlaybackHistory)
            {
                await ClearHistoryAsync();
                return;
            }

            if (limit <= 0)
            {
                return;
            }
            context.PlaybackHistory.RemoveRange(
                context.PlaybackHistory
                .OrderByDescending(item => item.PlaybackTime)
                .Skip(limit));
            await context.SaveChangesAsync();
        }

        private async Task AddHistoryInternal(DbPlaybackHistory history)
        {
            context.PlaybackHistory.Add(history);
            await context.SaveChangesAsync();
        }

        public Task AddHistoryAsync(MusicPlaybackItem item)
        {
            if (!EnablePlaybackHistory)
            {
                // User disabled history.
                return Task.CompletedTask;
            }

            if (LibraryService.IsIndexing)
            {
                return Task.CompletedTask;
            }

            var file = item.File;

            if (!file.IsExternal)
            {
                var fileExist = context.MediaFiles.Any(f => f.Id == file.Id);

                if (!fileExist)
                {
                    return Task.CompletedTask;
                }

                _historyCache.Insert(0, item);
                NewEntryAdded?.Invoke(this, item);
                return AddHistoryInternal(new DbPlaybackHistory
                {
                    RelatedMediaFileId = file.Id,
                    PlaybackTime = DateTimeOffset.Now
                });
            }
            else
            {
                return Task.CompletedTask;
            }
        }
    }
}
