using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncIO
{
    public static class Tasks
    {


        /// <summary>
        /// Returns the content of required uris.
        /// Method has to use the synchronous way and can be used to compare the performace of sync \ async approaches. 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContent(this IEnumerable<Uri> uris)
        {
            return uris.Select(x => new WebClient().DownloadString(x));
        }



        /// <summary>
        /// Returns the content of required uris.
        /// Method has to use the asynchronous way and can be used to compare the performace of sync \ async approaches. 
        /// 
        /// maxConcurrentStreams parameter should control the maximum of concurrent streams that are running at the same time (throttling). 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <param name="maxConcurrentStreams">Max count of concurrent request streams</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContentAsync(this IEnumerable<Uri> uris, int maxConcurrentStreams)
        {
            int index = 0;
            var task = new Task<string>[maxConcurrentStreams];

            foreach (var uri in uris)
            {
                if (index >= maxConcurrentStreams)
                {
                    var indexCompletedTasks = Task.WaitAny(task);
                    var completedTasks = task[indexCompletedTasks];
                    task[indexCompletedTasks] = GetOneUri(uri);
                    yield return completedTasks.Result;
                }
                else
                {
                    task[index] = GetOneUri(uri);
                    index++;
                }
            }

            var listTasks = task.ToList();
            while (!listTasks.Any())
            {
                var indexTask = Task.WaitAny(listTasks.ToArray());
                var resultTask = listTasks[indexTask];
                listTasks.RemoveAt(indexTask);
                yield return resultTask.Result;
            }
        }
        private static Task<string> GetOneUri(Uri uri)
        {
            return new HttpClient().GetStringAsync(uri);
        }



        /// <summary>
        /// Calculates MD5 hash of required resource.
        /// 
        /// Method has to run asynchronous. 
        /// Resource can be any of type: http page, ftp file or local file.
        /// </summary>
        /// <param name="resource">Uri of resource</param>
        /// <returns>MD5 hash</returns>
        public static async Task<string> GetMD5Async(this Uri resource)
        {
            return string.Join("", await new WebClient().DownloadDataTaskAsync(resource)
                .ContinueWith(x => MD5.Create().ComputeHash(x.Result).Select(y => y.ToString("x2"))));
        }

    }
}
