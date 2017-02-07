using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using TwitterWebApp.Models;

namespace TwitterWebApp.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Assigning Credentials for integrating Twitter API
        /// </summary>
        string CONSUMER_KEY = ConfigurationManager.AppSettings["token_ConsumerKey"];
        string CONSUMER_SECRET = ConfigurationManager.AppSettings["token_ConsumerSecret"];
        string ACCESS_TOKEN = ConfigurationManager.AppSettings["token_AccessToken"];
        string ACCESS_TOKEN_SECRET = ConfigurationManager.AppSettings["token_AccessTokenSecret"];
        static IAuthenticationContext _authenticationContext = null;
        static IUser user = null;
        static IUser mainuser = null;

        static FormCollection lastSearch = null;
        /// <summary>
        /// Redirect user to go on Twitter.com to authenticate
        /// </summary>
        /// <returns></returns>
        public ActionResult TwitterAuth()
        {
            var appCreds = new ConsumerCredentials(CONSUMER_KEY, CONSUMER_SECRET);

            // Specify the url you want the user to be redirected to
            var redirectURL = "http://" + Request.Url.Authority + "/Home/ValidateTwitterAuth";
            _authenticationContext = AuthFlow.InitAuthentication(appCreds, redirectURL);

            return new RedirectResult(_authenticationContext.AuthorizationURL);
        }

        /// <summary>
        /// Receives Token from Twitter and Validates the User Account
        /// </summary>
        /// <returns></returns>
        public ActionResult ValidateTwitterAuth()
        {
            // Get some information back from the URL
            var verifierCode = Request.Params.Get("oauth_verifier");

            // Create the user credentials
            var userCreds = AuthFlow.CreateCredentialsFromVerifierCode(verifierCode, _authenticationContext);

            // Do whatever you want with the user now!
            if (userCreds != null)
            {
                mainuser = (Tweetinvi.User.GetAuthenticatedUser(userCreds) as object) as IUser ;
                ViewData["USER"] = user;
                user = mainuser;
            }
            return ViewTweets();
        }

        /// <summary>
        /// Filters the Numbers of Tweets to be seen
        /// </summary>
        /// <param name="fc"></param>
        /// <returns></returns>
        public ActionResult NumTweets(FormCollection fc)
        {
            if (user == mainuser) { return ViewTweets(int.Parse(fc["txtNum"].ToString())); }
            return SearchTweets(lastSearch, int.Parse(fc["txtNum"].ToString()));
        }

        /// <summary>
        /// Tweet Post Status
        /// </summary>
        /// <param name="fm"></param>
        /// <returns></returns>
        public ActionResult PTweets(FormCollection fm)
        {
            var newTweet = fm["txtTweet"].ToString();
            if (Tweet.PublishTweet(newTweet) != null)
            {
                ViewBag.PostTweetStatus = "Tweet Posted Successfully.!!";
            }
            else
            {
                ViewBag.PostTweetStatus = "Error Occured while Tweeting";
            }
            return ViewTweets();
        }

        /// <summary>
        /// To Display the Tweets, we send List of ViewTweets 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public ActionResult ViewTweets(int num = 10)
        {
            Auth.SetUserCredentials(CONSUMER_KEY, CONSUMER_SECRET, ACCESS_TOKEN, ACCESS_TOKEN_SECRET);
            var timelineTweets = Timeline.GetUserTimeline(user, num);
            List<ViewTweet> VTList = new List<ViewTweet>();
            ViewTweet vt = null;
            foreach(var tweet in timelineTweets)
            {
                vt = new ViewTweet();
                vt.Name = user.ToString();
                vt.ScreenName = user.ScreenName;
                vt.TweetText = tweet.FullText;
                vt.ProfilePic = user.ProfileImageUrl;
                if (tweet.Media.Count() != 0)
                {
                    vt.MMedia = tweet.Media.FirstOrDefault().MediaURL;
                }
                VTList.Add(vt);
            }
            ViewData["USER"] = user;
            return View("PostTweets",VTList);
        }
   
        /// <summary>
        /// Searches for Tweets based on Constraints as specified like based on user,keyword and place/location
        /// </summary>
        /// <param name="fc"></param>
        /// <returns></returns>
        public ActionResult SearchTweets(FormCollection fc,int TweetCount=10)
        {
            lastSearch = fc;
            var rad = fc["rdbSearchType"].ToString();
            string searchWord = fc["txtSearchKey"].ToString();
            List<ViewTweet> VTList = new List<ViewTweet>();
            Auth.SetUserCredentials("qua2BV6iNd0vZScQoIWlpdvo1", "bRFd5MWIf6v7jWv9hoRDj63gO8AJ5acq2iKZxLxQuqOlul7N0f", "822187417952194561-QFcnGfDtUjHkIdh7XC4EuCl1FjmEbHe", "0FisdhapDwYZqkexRaIJLIICdimGqGs4Bf1KoMNLmjm1Q");
            if (rad == "By User")
            {
                var user1 = Search.SearchUsers(searchWord).FirstOrDefault();
                user = user1;
                var timelineTweets = Timeline.GetUserTimeline(user1, TweetCount);// default count is 10
                VTList = GetListOfTweets(timelineTweets,user1);
                ViewData["USER"] = user1;
            }

            if(rad == "By Keyword")
            {
                var timelineTweets = Search.SearchTweets(searchWord);
                timelineTweets = timelineTweets.Take(TweetCount);
                VTList = GetListOfTweets(timelineTweets);
                ViewData["USER"] = mainuser;
            }

            if (rad == "By Location")
            {
                double lat = double.Parse(fc["Latitude"].ToString());
                double lng = double.Parse(fc["Longitude"].ToString());
                var tweets = Search.SearchTweets(new SearchTweetsParameters(searchWord)
                {
                    GeoCode = new GeoCode(lat, lng, 10, DistanceMeasure.Miles),
                    FilterTweetsNotContainingGeoInformation = true
                });
                tweets = tweets.Take(TweetCount);
                VTList = GetListOfTweets(tweets);
                ViewData["USER"] = mainuser;
            }
                return View("PostTweets", VTList);
        }

        /// <summary>
        /// returns a list of ViewTweets
        /// </summary>
        /// <param name="bunchOfTweets"></param>
        /// <param name="user1"></param>
        /// <returns></returns>
        List<ViewTweet> GetListOfTweets(IEnumerable<ITweet> bunchOfTweets,IUser user1=null)
        {
            List<ViewTweet> ListOfTweets = new List<ViewTweet>();
            ViewTweet vt = null;
            foreach (var tweet in bunchOfTweets)
            {
                vt = new ViewTweet();
                user1 = tweet.CreatedBy;
                vt.TweetText = tweet.FullText;
                if (tweet.Urls.Count() != 0)
                {
                    vt.Link = tweet.Urls.FirstOrDefault().URL;
                    vt.TweetText = tweet.FullText.Replace(vt.Link, "");
                }
                vt.ProfilePic = user1.ProfileImageUrl;
                vt.Name = user1.ToString();
                vt.ScreenName = user1.ScreenName;
                vt.CreatedTime = tweet.CreatedAt;
                if (tweet.Media.Count() != 0)
                {
                    vt.MMedia = tweet.Media.FirstOrDefault().MediaURL;
                }
                ListOfTweets.Add(vt);
            }
            return ListOfTweets;
        }

    }
}