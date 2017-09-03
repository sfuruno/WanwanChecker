using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WanwanChecker
{
    public static class TimerScrape
    {
        [FunctionName("TimerScrape")]
        //public static void Run([TimerTrigger("0 0 */6 * * *")]TimerInfo myTimer, TraceWriter log)
        public static async Task Run([TimerTrigger("5 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            try
            {
                var document = await Scrape(GetAddress());
                var rows = document.QuerySelectorAll(GetRowSelector());
                //var row = rows.LastOrDefault();
                var row = rows.ElementAt(7);
                var imgElement = row?.LastElementChild?.LastElementChild?.LastElementChild as IHtmlImageElement;

                if (imgElement?.AlternativeText != "���ē���")
                {
                    log.Info($"��t���n�܂��ĂȂ����ߏI��");
                    return;
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }

            // Slack�ւ̒ʒm
            var payload = GetSlackPayload();
            var endpoint = GetSlackEndpoint();
            var httpClient = new HttpClient();
            await httpClient.PostAsync(endpoint, new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("payload", payload)
            }));
        }

        /// <summary>
        /// �X�N���C�s���O���s���܂��B
        /// </summary>
        /// <param name="address">�X�N���C�s���O���s���A�h���X</param>
        /// <returns>�X�N���C�s���O���s���� IDocument</returns>
        private static async Task<IDocument> Scrape(string address)
        {
            var config = AngleSharp.Configuration.Default.WithDefaultLoader();
            var document = await BrowsingContext.New(config).OpenAsync(address);
            return document;
        }

        /// <summary>
        /// �A�h���X���擾���܂��B
        /// </summary>
        /// <returns>�A�h���X</returns>
        private static string GetAddress()
        {
            return ConfigurationManager.AppSettings["Address"];
        }

        /// <summary>
        /// �Z���N�^���擾���܂��B
        /// </summary>
        /// <returns>�Z���N�^</returns>
        private static string GetRowSelector()
        {
            return ConfigurationManager.AppSettings["RowSelector"];
        }

        /// <summary>
        /// Slack�̒ʒm�����擾���܂��B
        /// </summary>
        /// <returns>�ʒm���</returns>
        private static string GetSlackPayload()
        {
            var payload = new
            {
                attachments = new []
                {
                    new
                    {
                        title = "����������񂾁[���ǂ̎�t���n�܂�����",
                        title_link = GetAddress(),
                    }
                }
            };
            return JsonConvert.SerializeObject(payload);
        }

        /// <summary>
        /// Slack�̃G���h�|�C���g���擾���܂��B
        /// </summary>
        /// <returns>�G���h�|�C���g</returns>
        private static string GetSlackEndpoint()
        {
            return ConfigurationManager.AppSettings["SlackEndpoint"];
        }
    }
}
