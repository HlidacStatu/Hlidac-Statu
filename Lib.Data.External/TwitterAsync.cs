using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToTwitter;
using LinqToTwitter.OAuth;

namespace HlidacStatu.Lib.Data.External
{

    public class Twitter
    {
        static SingleUserAuthorizer authHlidacW = new SingleUserAuthorizer
        {
            CredentialStore = new SingleUserInMemoryCredentialStore
            {

                ConsumerKey = Devmasters.Config.GetWebConfigValue("hlidacw-twitter-api-key"),
                ConsumerSecret = Devmasters.Config.GetWebConfigValue("hlidacw-twitter-api-secret"),
                AccessToken = Devmasters.Config.GetWebConfigValue("hlidacw-twitter-api-accesstoken"),
                AccessTokenSecret = Devmasters.Config.GetWebConfigValue("hlidacw-twitter-api-tokensecret")
            }
        };
        static SingleUserAuthorizer authHlidacStatu = new SingleUserAuthorizer
        {
            CredentialStore = new SingleUserInMemoryCredentialStore
            {
                //Devmasters.Config.GetWebConfigValue("twitter-api-key"),
                //Devmasters.Config.GetWebConfigValue("twitter-api-secret"),
                //Devmasters.Config.GetWebConfigValue("twitter-api-accesstoken"),
                //Devmasters.Config.GetWebConfigValue("twitter-api-tokensecret")

                ConsumerKey = Devmasters.Config.GetWebConfigValue("hlidacstatu-twitter-api-key"),
                ConsumerSecret = Devmasters.Config.GetWebConfigValue("hlidacstatu-twitter-api-secret"),
                AccessToken = Devmasters.Config.GetWebConfigValue("hlidacstatu-twitter-api-accesstoken"),
                AccessTokenSecret = Devmasters.Config.GetWebConfigValue("hlidacstatu-twitter-api-tokensecret"), 
                  
            }
        };

        public enum TwAccount
        { 
            HlidacStatu,
            HlidacW
        }


        private readonly TwitterContext context = null;

        public TwitterContext Context { get => context; }

        public Twitter(TwAccount acc)
        {
            //authHlidacW.Proxy = new System.Net.WebProxy("127.0.0.1", 8888);
            switch (acc)
            {
                case TwAccount.HlidacStatu:
                    context = new TwitterContext(authHlidacStatu);
                    break;
                case TwAccount.HlidacW:
                default:
                    context = new TwitterContext(authHlidacW);
                    break;
            }

        }
        public async Task<string> NewTweetAsync(string text)
        {
            try
            {
                var tweet = await context.TweetAsync(text);
                return tweet?.ID;

            }
            catch (Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("tweet creation. text {text}, error {error}", text, e);
                return null;
            }

        }
        public async Task<string> NewThreadAsync(params string[] texts)
        {
            if (texts == null)
                return null;
            if (texts.Length == 0)
                return null;

            if (texts.Length == 1)
                return await NewTweetAsync(texts[0]);

            var tweetId = await NewTweetAsync(texts[0]);
            foreach (var text in texts.Skip(1))
            {
                tweetId = await ReplyAsync(text, tweetId);
                if (string.IsNullOrEmpty(tweetId))
                {
                    HlidacStatu.Util.Consts.Logger.Error("Error during tweet thread creation. all {texts}, error after {text}.",texts, text);
                    return null;
                }
                System.Threading.Thread.Sleep(20);
            }

            return tweetId;
        }


        public async Task<string> ReplyAsync(string text, string replyToTweetId)
        {
            try { 
            var tweet = await context.ReplyAsync(text, replyToTweetId);

            return tweet?.ID;

            }
            catch (Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("tweet reply. text {text}, error {error}", text, e);
                return null;
            }

        }


    }
}
