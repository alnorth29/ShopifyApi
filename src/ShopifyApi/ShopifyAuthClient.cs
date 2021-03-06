﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetOpenAuth.OAuth2;
using System.Diagnostics;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using DotNetOpenAuth.OpenId.Extensions.OAuth;

namespace Shopify
{
    public class ShopifyAuthClient : WebServerClient
    {
        public string ShopName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookClient"/> class.
        /// </summary>
        public ShopifyAuthClient(string shopName, string clientID, string clientSecret)
            : base(getAuthorizationServerDescription(shopName))
        {
            this.ShopName = shopName;
            this.ClientIdentifier = clientID;
            this.ClientSecret = clientSecret;
            this.AuthorizationTracker = new InMemoryTokenManager(clientID, clientSecret);
        }
        
        private static AuthorizationServerDescription getAuthorizationServerDescription(string shopName)
        {
            return new AuthorizationServerDescription
            {
                TokenEndpoint = new Uri(String.Format("https://{0}.myshopify.com/admin/oauth/access_token", shopName)),
                AuthorizationEndpoint = new Uri(String.Format("https://{0}.myshopify.com/admin/oauth/authorize", shopName)),
            };
        }


        public ShopifyAuthorizationState ProcessAuthorization()
        {
            return new ShopifyAuthorizationState(this.ShopName, this.ProcessUserAuthorization());
        }

    }


    /// <summary>
    /// Wrapper around the built in Authorization State in order to 
    /// hold Shopify specific State
    /// </summary>
    public class ShopifyAuthorizationState : IAuthorizationState
    {
        public ShopifyAuthorizationState(string shopName, IAuthorizationState internalState)
            : base()
        {
            this._shopName = shopName;
            this._internal = internalState;
        }

        private IAuthorizationState _internal;
        private string _shopName;

        public string ShopName
        {
            get
            {
                return _shopName;
            }
        }

        #region IAuthorizationState Members

        public string AccessToken
        {
            get
            {
                if (this._internal == null)
                    return null;
                return this._internal.AccessToken;
            }
            set
            {
                this._internal.AccessToken = value;
            }
        }

        public DateTime? AccessTokenExpirationUtc
        {
            get
            {
                if (this._internal == null)
                    return null;
                return this._internal.AccessTokenExpirationUtc;
            }
            set
            {
                this._internal.AccessTokenExpirationUtc = value;
            }
        }

        public DateTime? AccessTokenIssueDateUtc
        {
            get
            {
                if (this._internal == null)
                    return null;
                return this._internal.AccessTokenIssueDateUtc;
            }
            set
            {
                this._internal.AccessTokenIssueDateUtc = value;
            }

        }

        public Uri Callback
        {
            get
            {
                if (this._internal == null)
                    return null;
                return this._internal.Callback;
            }
            set
            {
                this._internal.Callback = value;
            }

        }

        public void Delete()
        {

            if (this._internal == null)
                return;
            this._internal.Delete();

        }

        public string RefreshToken
        {
            get
            {
                if (this._internal == null)
                    return null;
                return this._internal.RefreshToken;
            }
            set
            {
                this._internal.RefreshToken = value;
            }

        }

        public void SaveChanges()
        {

            if (this._internal == null)
                return; ;

            this._internal.SaveChanges();
        }

        public HashSet<string> Scope
        {
            get
            {
                if (this._internal == null)
                    return null;
                return this._internal.Scope;
            }

        }

        #endregion
    }
     
    /// <summary>
    /// A token manager that only retains tokens in memory. 
    /// Meant for SHORT TERM USE TOKENS ONLY.
    /// </summary>
    /// <remarks>
    /// A likely application of this class is for "Sign In With Twitter",
    /// where the user only signs in without providing any authorization to access
    /// Twitter APIs except to authenticate, since that access token is only useful once.
    /// </remarks>
    public class InMemoryTokenManager : IConsumerTokenManager, IClientAuthorizationTracker
    {
        private Dictionary<string, string> tokensAndSecrets = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryTokenManager"/> class.
        /// </summary>
        /// <param name="consumerKey">The consumer key.</param>
        /// <param name="consumerSecret">The consumer secret.</param>
        public InMemoryTokenManager(string consumerKey, string consumerSecret)
        {
            if (string.IsNullOrEmpty(consumerKey))
            {
                throw new ArgumentNullException("consumerKey");
            }

            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
        }

        /// <summary>
        /// Gets the consumer key.
        /// </summary>
        /// <value>The consumer key.</value>
        public string ConsumerKey { get; private set; }

        /// <summary>
        /// Gets the consumer secret.
        /// </summary>
        /// <value>The consumer secret.</value>
        public string ConsumerSecret { get; private set; }

        #region ITokenManager Members

        /// <summary>
        /// Gets the Token Secret given a request or access token.
        /// </summary>
        /// <param name="token">The request or access token.</param>
        /// <returns>
        /// The secret associated with the given token.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the secret cannot be found for the given token.</exception>
        public string GetTokenSecret(string token)
        {
            return this.tokensAndSecrets[token];
        }

        /// <summary>
        /// Stores a newly generated unauthorized request token, secret, and optional
        /// application-specific parameters for later recall.
        /// </summary>
        /// <param name="request">The request message that resulted in the generation of a new unauthorized request token.</param>
        /// <param name="response">The response message that includes the unauthorized request token.</param>
        /// <exception cref="ArgumentException">Thrown if the consumer key is not registered, or a required parameter was not found in the parameters collection.</exception>
        /// <remarks>
        /// Request tokens stored by this method SHOULD NOT associate any user account with this token.
        /// It usually opens up security holes in your application to do so.  Instead, you associate a user
        /// account with access tokens (not request tokens) in the <see cref="ExpireRequestTokenAndStoreNewAccessToken"/>
        /// method.
        /// </remarks>
        public void StoreNewRequestToken(UnauthorizedTokenRequest request, ITokenSecretContainingMessage response)
        {
            this.tokensAndSecrets[response.Token] = response.TokenSecret;
        }

        /// <summary>
        /// Deletes a request token and its associated secret and stores a new access token and secret.
        /// </summary>
        /// <param name="consumerKey">The Consumer that is exchanging its request token for an access token.</param>
        /// <param name="requestToken">The Consumer's request token that should be deleted/expired.</param>
        /// <param name="accessToken">The new access token that is being issued to the Consumer.</param>
        /// <param name="accessTokenSecret">The secret associated with the newly issued access token.</param>
        /// <remarks>
        /// 	<para>
        /// Any scope of granted privileges associated with the request token from the
        /// original call to <see cref="StoreNewRequestToken"/> should be carried over
        /// to the new Access Token.
        /// </para>
        /// 	<para>
        /// To associate a user account with the new access token,
        /// <see cref="System.Web.HttpContext.User">HttpContext.Current.User</see> may be
        /// useful in an ASP.NET web application within the implementation of this method.
        /// Alternatively you may store the access token here without associating with a user account,
        /// and wait until <see cref="WebConsumer.ProcessUserAuthorization()"/> or
        /// <see cref="DesktopConsumer.ProcessUserAuthorization(string, string)"/> return the access
        /// token to associate the access token with a user account at that point.
        /// </para>
        /// </remarks>
        public void ExpireRequestTokenAndStoreNewAccessToken(string consumerKey, string requestToken, string accessToken, string accessTokenSecret)
        {
            this.tokensAndSecrets.Remove(requestToken);
            this.tokensAndSecrets[accessToken] = accessTokenSecret;
        }

        /// <summary>
        /// Classifies a token as a request token or an access token.
        /// </summary>
        /// <param name="token">The token to classify.</param>
        /// <returns>Request or Access token, or invalid if the token is not recognized.</returns>
        public TokenType GetTokenType(string token)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region IClientAuthorizationTracker Members



        public IAuthorizationState GetAuthorizationState(Uri callbackUrl, string clientState)
        {
            return new AuthorizationState
            {
                Callback = callbackUrl               
            };
        }

        #endregion
    }
}
