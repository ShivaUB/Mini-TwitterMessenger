using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tweetinvi;

namespace TwitterWebApp.Models
{
    public class ViewTweet
    {
        public string ProfilePic { get; set; }
        public string Link { get; set; }
        public string Name { get; set; }
        public string ScreenName { get; set; }
        public string TweetText { get; set; }
        public string MMedia { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}