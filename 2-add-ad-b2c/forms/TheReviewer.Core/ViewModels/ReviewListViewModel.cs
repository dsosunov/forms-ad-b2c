﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;

using Xamarin.Forms;

using MvvmHelpers;
using Newtonsoft.Json;
using Microsoft.Identity.Client;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace TheReviewer.Core
{
    public class ReviewListViewModel : BaseViewModel
    {
        //string location = "http://localhost:5000";
        string location = "http://thereviewer.azurewebsites.net";

        IIdentityService login;

        string accessToken;

        public ReviewListViewModel()
        {
            login = DependencyService.Get<IIdentityService>(DependencyFetchTarget.GlobalInstance);

            Title = "All Reviews";

            Task.Run(async () =>
            {
                var result = await login.GetCachedSignInToken();
                accessToken = result?.AccessToken;
                LoggedOut = string.IsNullOrWhiteSpace(result?.AccessToken);

                if (!LoggedOut)
                    RefreshCommand.Execute(null);
            });
        }

        bool _loggedOut = true;
        public bool LoggedOut
        {
            get => _loggedOut;
            set
            {
                SetProperty(ref _loggedOut, value);
                LoginCommand.ChangeCanExecute();
            }
        }

        public ObservableRangeCollection<Review> AllReviews { get; set; } = new ObservableRangeCollection<Review>();

        Command _refreshCommand;
        public Command RefreshCommand => _refreshCommand ??
        (_refreshCommand = new Command(async () =>
        {
            if (LoggedOut)
                return;

            try
            {
                var baseAddr = new Uri(location);
                var client = new HttpClient { BaseAddress = baseAddr };

                var reviewUri = new Uri(baseAddr, "api/reviews");
                var request = new HttpRequestMessage(HttpMethod.Get, reviewUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var reviewJson = await response.Content.ReadAsStringAsync();

                var allReviews = JsonConvert.DeserializeObject<List<Review>>(reviewJson);

                AllReviews.AddRange(allReviews);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }));

        Command _loginCommand;
        public Command LoginCommand => _loginCommand ??
        (_loginCommand = new Command(async () =>
        {
            try
            {
                // Calling the AD B2C sign in or sign up policy
                var login = DependencyService.Get<IIdentityService>(DependencyFetchTarget.GlobalInstance);
                var authResponse = await login.Login();
                accessToken = authResponse.AccessToken;

                LoggedOut = string.IsNullOrWhiteSpace(authResponse?.AccessToken);
            }
            catch (Exception ex)
            {
                LoggedOut = true;
                Console.WriteLine(ex);
            }
        }, () => LoggedOut));
    }
}
