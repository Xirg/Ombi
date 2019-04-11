﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Ombi.Api.Service;
using Ombi.Attributes;
using Ombi.Helpers;
using Ombi.Schedule;
using Ombi.Schedule.Jobs;
using Ombi.Schedule.Jobs.Emby;
using Ombi.Schedule.Jobs.Ombi;
using Ombi.Schedule.Jobs.Plex;
using Quartz;

namespace Ombi.Controllers
{
    [ApiV1]
    [Admin]
    [Produces("application/json")]
    [ApiController]
    public class JobController : ControllerBase
    {
        public JobController(IOmbiAutomaticUpdater updater, IPlexUserImporter userImporter,
            ICacheService mem, IEmbyUserImporter embyImporter, IPlexContentSync plexContentSync,
            IEmbyContentSync embyContentSync, INewsletterJob newsletter)
        {
            _updater = updater;
            _plexUserImporter = userImporter;
            _embyUserImporter = embyImporter;
            _memCache = mem;
            _plexContentSync = plexContentSync;
            _embyContentSync = embyContentSync;
            _newsletterJob = newsletter;
        }

        private readonly IOmbiAutomaticUpdater _updater;
        private readonly IPlexUserImporter _plexUserImporter;
        private readonly IEmbyUserImporter _embyUserImporter;
        private readonly ICacheService _memCache;
        private readonly IPlexContentSync _plexContentSync;
        private readonly IEmbyContentSync _embyContentSync;
        private readonly INewsletterJob _newsletterJob;

        /// <summary>
        /// Runs the update job
        /// </summary>
        /// <returns></returns>
        [HttpPost("update")]
        public async Task<bool> ForceUpdate()
        {

            await OmbiQuartz.TriggerJob(nameof(IOmbiAutomaticUpdater));
            return true;
        }

        /// <summary>
        /// Checks for an update
        /// </summary>
        /// <returns></returns>
        [HttpGet("update")]
        public async Task<bool> CheckForUpdate()
        {
            try
            {
                var productArray = _updater.GetVersion();
                var version = productArray[0];
                var branch = productArray[1];
                var updateAvailable = await _updater.UpdateAvailable(branch, version);

                return updateAvailable;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("updateCached")]
        public async Task<bool> CheckForUpdateCached()
        {
            var val = await _memCache.GetOrAdd(CacheKeys.Update, async () =>
            {
                var productArray = _updater.GetVersion();
                var version = productArray[0];
                var branch = productArray[1];
                var updateAvailable = await _updater.UpdateAvailable(branch, version);

                return updateAvailable;
            });
            return val;
        }

        /// <summary>
        /// Runs the Plex User importer
        /// </summary>
        /// <returns></returns>
        [HttpPost("plexuserimporter")]
        public async Task<bool> PlexUserImporter()
        {
            await OmbiQuartz.TriggerJob(nameof(IPlexUserImporter));
            return true;
        }

        /// <summary>
        /// Runs the Emby User importer
        /// </summary>
        /// <returns></returns>
        [HttpPost("embyuserimporter")]
        public async Task<bool> EmbyUserImporter()
        {
            await OmbiQuartz.TriggerJob(nameof(IEmbyUserImporter));
            return true;
        }

        /// <summary>
        /// Runs the Plex Content Cacher
        /// </summary>
        /// <returns></returns>
        [HttpPost("plexcontentcacher")]
        public bool StartPlexContentCacher()
        {
            OmbiQuartz.Scheduler.TriggerJob(new JobKey(nameof(PlexContentSync)), new JobDataMap(new Dictionary<string, string> { { "recentlyAddedSearch", "false" } }));
            return true;
        }

        /// <summary>
        /// Runs a smaller version of the content cacher
        /// </summary>
        /// <returns></returns>
        [HttpPost("plexrecentlyadded")]
        public bool StartRecentlyAdded()
        {
            OmbiQuartz.Scheduler.TriggerJob(new JobKey(nameof(PlexContentSync)), new JobDataMap(new Dictionary<string, string> { { "recentlyAddedSearch", "true" } }));
            return true;
        }

        /// <summary>
        /// Runs the Emby Content Cacher
        /// </summary>
        /// <returns></returns>
        [HttpPost("embycontentcacher")]
        public async Task<bool> StartEmbyContentCacher()
        {
            await OmbiQuartz.TriggerJob(nameof(IEmbyContentSync));
            return true;
        }

        /// <summary>
        /// Runs the newsletter
        /// </summary>
        /// <returns></returns>
        [HttpPost("newsletter")]
        public async Task<bool> StartNewsletter()
        {
            await OmbiQuartz.TriggerJob(nameof(INewsletterJob));
            return true;
        }
    }
}