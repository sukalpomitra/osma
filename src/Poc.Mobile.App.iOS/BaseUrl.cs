﻿using System;
using Foundation;
using Poc.Mobile.App.iOS;

[assembly: Xamarin.Forms.Dependency(typeof(BaseUrl))]
namespace Poc.Mobile.App.iOS
{
    public class BaseUrl: IBaseUrl
    {
        public string Get()
        {
            return NSBundle.MainBundle.BundlePath;
        }
    }
}
