using IBSMobile.Data;
using IBSMobile.DTOs;
using IBSMobile.Models;
using IBSMobile.Services;
using IBSMobile.Statics;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Net;


namespace IBSMobile.Functions
{
    public class IBSFunctions
    {
        private const string BearerPrefix = "Bearer ";

        private readonly ApplicationDbContext _context;
        private readonly RestClient _apiRestClient;
        private readonly string _baseUri;
        private string? _cachedConString;
        private int? _cachedUserAppUserId;

        public IBSFunctions(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _baseUri = ApiServerUriResolver.Resolve(context, configuration);
            _apiRestClient = new RestClient(new RestClientOptions(_baseUri)
            {
                Timeout = Timeout.InfiniteTimeSpan
            });
        }

        private async Task<int> GetUserAppUserIdAsync()
        {
            if (_cachedUserAppUserId.HasValue)
                return _cachedUserAppUserId.Value;

            var userApp = await _context.UserApp.FirstOrDefaultAsync();
            if (userApp == null)
                throw new InvalidOperationException("اعدادت التطبيق غير صحيحة.");

            _cachedUserAppUserId = userApp.UserId;
            return userApp.UserId;
        }

        private RestRequest CreateApiRequest(string path, int userAppUserId)
        {
            var request = new RestRequest(path, Method.Post);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("user_id", userAppUserId.ToString());
            request.AddHeader("source_Id", "web-app");
            request.AddHeader("ConnectionStr", GetConString());
            return request;
        }

        private async Task<RestResponse> PostToApiAsync(string path, string jsonBody)
        {
            var userAppUserId = await GetUserAppUserIdAsync();
            var request = CreateApiRequest(path, userAppUserId);
            request.AddJsonBody(jsonBody);
            return await _apiRestClient.ExecuteAsync(request);
        }

        private static Response ErrorResult(string message) => new() { error = true, message = message };

        private async Task<Response> ExecuteWithUnauthorizedRetryAsync(
            Func<Task<string>> getTokenAsync,
            string path,
            Func<string, string> buildJsonBody)
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var token = await getTokenAsync();
                if (token.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                    return ErrorResult(token);

                var response = await PostToApiAsync(path, buildJsonBody(token));
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (attempt == 1)
                        return ErrorResult("unauthorized");
                    continue;
                }

                if (response.Content == null)
                    return ErrorResult("error: an error occurred.");

                try
                {
                    return JsonConvert.DeserializeObject<Response>(response.Content)
                        ?? ErrorResult("error: an error occurred.");
                }
                catch (Exception ex)
                {
                    return ErrorResult(ex.Message);
                }
            }

            return ErrorResult("unauthorized");
        }

        private async Task<Response> ParseFtthResponseAsync(string? stringResult, int affiliate)
        {
            if (stringResult == null)
                return ErrorResult("لا يوجد رد من الخادم. الرجاء مراجعة الصفحة في حال تم التفعيل بدون حفظ أو لا");

            if (!stringResult.Contains("error", StringComparison.OrdinalIgnoreCase))
                return new Response { error = false, data = stringResult };

            try
            {
                var result = JsonConvert.DeserializeObject<Response>(stringResult);
                if (result == null)
                    return ErrorResult("حدث خطأ أثناء قراءة الرد من الخادم. الرجاء مراجعة الصفحة في حال تم التفعيل أو لا");

                if (result.error && result.message?.Contains("authorize", StringComparison.OrdinalIgnoreCase) == true)
                    await RemoveCurrentToken(affiliate);

                return result;
            }
            catch
            {
                return ErrorResult("حدث خطأ أثناء قراءة الرد من الخادم. الرجاء مراجعة الصفحة في حال تم التفعيل أو لا");
            }
        }

        private static string BoolJson(bool value) => value ? "true" : "false";



        public async Task<AuthResponseDto> GetUserFormTokenAsync(string token)
        {
            if (token.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
                token = token[BearerPrefix.Length..];

            var handler = new JwtSecurityTokenHandler();
            if (handler.ReadToken(token) is not JwtSecurityToken jwtToken)
            {
                return new AuthResponseDto
                {
                    isSuccess = false,
                    error = "أنت غير مرخص لاكمال هذا الطلب"
                };
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || userIdClaim == "0" || !int.TryParse(userIdClaim, out var clientId) || clientId == 0)
            {
                return new AuthResponseDto
                {
                    isSuccess = false,
                    error = "أنت غير مرخص لاكمال هذا الطلب"
                };
            }

            var acToken = await _context.SubscriberAccessTokens.FirstOrDefaultAsync(x => x.accessToken == token);
            if (acToken == null)
            {
                return new AuthResponseDto
                {
                    isSuccess = false,
                    error = "أنت غير مرخص لاكمال هذا الطلب"
                };
            }

            if (acToken.ExpirationDate < DateTime.Now)
            {
                return new AuthResponseDto
                {
                    isSuccess = false,
                    error = "تم انتهاء الجلسة."
                };
            }

            var responseUser = await _context.Subscribers.FirstOrDefaultAsync(x => x.Id == acToken.SubscriberId);
            if (responseUser == null || clientId != responseUser.Id)
            {
                return new AuthResponseDto
                {
                    isSuccess = false,
                    error = "اسم المستخدم أو كلمة المرور غير صحيحة."
                };
            }

            if (!responseUser.IsValid)
            {
                return new AuthResponseDto
                {
                    isSuccess = false,
                    error = "حسابك معلق."
                };
            }

            return new AuthResponseDto
            {
                isSuccess = true,
                accessToken = token,
                issuedAt = acToken.ExpirationDate.AddDays(-1),
                expiresAt = acToken.ExpirationDate,
                userId = responseUser.Id,
                fullName = responseUser.NameStr,
                mobile = "0" + responseUser.Mobile.ToString("0")
            };
        }




        #region earthlink

        public async Task<string> EarthlinkToken(int affiliate)
        {
            var tokenData = await _context.RefreshTokens.FirstOrDefaultAsync(m => m.Affiliate == affiliate);
            if (tokenData == null) return "error: no affiliate found.";
            if (tokenData.AccessTokenExpire > DateTime.Now && tokenData.AccessToken.Length > 20)
                return tokenData.AccessToken.Replace("\"", "");

            var mAffiliate = await _context.MainAffiliates.FirstOrDefaultAsync(m => m.Id == affiliate);
            if (mAffiliate == null) return "error: no affiliate found.";

            var response = await PostToApiAsync(
                "/api/Earthlink/GetEarthTokenData",
                $"{{ \"username\": \"{mAffiliate.Username}\", \"password\": \"{mAffiliate.Password}\" }}");
            var stringResult = response.Content;

            try
            {
                var result = JsonConvert.DeserializeObject<DtoEarthAccTokenData>(stringResult);
                if (result?.access_token == null)
                {
                    tokenData.AccessTokenExpire = DateTime.Now.AddYears(-1);
                    tokenData.AccessToken = "parse_error";
                    await _context.SaveChangesAsync();
                    return "error";
                }

                var accessToken = result.access_token.Replace("\"", "");

                if (accessToken == "error" || accessToken == "error: unknown error 502")
                {
                    tokenData.AccessTokenExpire = DateTime.Now.AddYears(-1);
                    tokenData.AccessToken = accessToken;
                    await _context.SaveChangesAsync();
                    return "error";
                }

                if (result.expires == null)
                {
                    tokenData.AccessTokenExpire = DateTime.Now.AddYears(-1);
                    tokenData.AccessToken = "expire_error";
                    await _context.SaveChangesAsync();
                    return "error";
                }

                tokenData.AccessTokenExpire = result.expires.Value;
                tokenData.AccessToken = accessToken;
                await _context.SaveChangesAsync();
                return accessToken;
            }
            catch
            {
                tokenData.AccessTokenExpire = DateTime.Now.AddYears(-1);
                tokenData.AccessToken = "parse_error";
                await _context.SaveChangesAsync();
                return "error";
            }
        }

        public async Task<string> EarthGetExpiration(int affiliate, decimal userIndex)
        {
            var etoken = await EarthlinkToken(affiliate);
            if (etoken.StartsWith("error", StringComparison.OrdinalIgnoreCase)) return "token_error";

            var response = await PostToApiAsync(
                "/api/Earthlink/GetExpiration",
                $"{{ \"token\": \"{etoken}\", \"userIndex\": {userIndex} }}");

            return response.Content ?? "no_result";
        }

        public Task<Response> EarthlinkGetUserDataByIndexWithPassword(int affiliate, decimal userIndex) =>
            ExecuteWithUnauthorizedRetryAsync(
                () => EarthlinkToken(affiliate),
                "/api/Earthlink/GetUserDataByIndexWithPassword",
                token => $"{{ \"token\": \"{token}\", \"userIndex\": {userIndex} }}");

        public async Task<Response> EarthlinkGetUserDataByUsernameWithPassword(int affiliate, string username)
        {
            var result = await ExecuteWithUnauthorizedRetryAsync(
                () => EarthlinkToken(affiliate),
                "/api/Earthlink/GetUserDataByUsernameWithPassword",
                token => $"{{ \"token\": \"{token}\", \"userId\": \"{username}\" }}");

            if (!result.error)
                return new Response { error = false, data = result.data };

            return result;
        }

         public async Task<Response> EarthRefillUser(DtoRefillWithDeposit form, int affiliate)
        {
            var etoken = await EarthlinkToken(affiliate);
            if (etoken.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                return new Response
                {
                    error = true,
                    message = etoken.Length > 6 ? etoken[6..] : etoken
                };

            int cost = (int)form.cost;
            int receive = (int)form.received;

            var response = await PostToApiAsync(
                "/api/Earthlink/RefillUserWithDeposit",
                $"{{ \"token\": \"{etoken}\", " +
                $"\"DepositPassword\": \"{form.DepositPassword}\", " +
                $"\"connectionString\": \"{form.connectionString}\", " +
                $"\"username\": \"{form.username}\", " +
                $"\"agent\": {form.agent}, " +
                $"\"userId\": {form.userId}, " +
                $"\"emp\": {form.emp}, " +
                $"\"details\": \"{form.details}\", " +
                $"\"profileId\": {form.profileId}, " +
                $"\"subAffiliate\": {form.subAffiliate}, " +
                $"\"saleType\": {BoolJson(form.saleType)}, " +
                $"\"cost\": {cost}, " +
                $"\"received\": {receive}, " +
                $"\"cashAccount\": {form.cashAccount}, " +
                $"\"saveInvoice\": {BoolJson(form.saveInvoice)} }}");

            var stringResult = response.Content;
            if (stringResult == null)
                return ErrorResult("لا يوجد رد من الخادم. الرجاء مراجعة الصفحة في حال تم التفعيل بدون حفظ أو لا");

            try
            {
                return JsonConvert.DeserializeObject<Response>(stringResult)
                    ?? ErrorResult("حدث خطأ أثناء قراءة الرد من الخادم. الرجاء مراجعة الصفحة في حال تم التفعيل أو لا");
            }
            catch
            {
                return ErrorResult("حدث خطأ أثناء قراءة الرد من الخادم. الرجاء مراجعة الصفحة في حال تم التفعيل أو لا");
            }
        }


        #endregion



        #region SAS

        public async Task<string> SASToken(int affiliate)
        {
            var tokenData = await _context.RefreshTokens.FirstOrDefaultAsync(m => m.Affiliate == affiliate);
            if (tokenData == null) return "error: no affiliate found.";
            if (tokenData.AccessTokenExpire > DateTime.Now) return tokenData.AccessToken;

            var mAffiliate = await _context.MainAffiliates.FirstOrDefaultAsync(m => m.Id == affiliate);
            if (mAffiliate == null) return "error: no affiliate found.";

            var response = await PostToApiAsync(
                "/api/SAS/Token",
                $"{{ \"username\": \"{mAffiliate.Username}\", \"password\": \"{mAffiliate.Password}\", \"uri\": \"{mAffiliate.Uri}\" }}");
            var stringResult = response.Content;

            if (stringResult == null) return "error: an error occurred.";

            try
            {
                var result = JsonConvert.DeserializeObject<Response>(stringResult);
                if (result == null || result.error)
                    return "error: " + (result?.message ?? "invalid username and password.");

                tokenData.AccessToken = result.data.ToString()!.Replace("\"", "");
                tokenData.AccessTokenExpire = DateTime.Now.AddHours(1).AddSeconds(-10);
                await _context.SaveChangesAsync();

                return tokenData.AccessToken;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> SASGetExpiration(int affiliate, decimal userIndex)
        {
            var etoken = await SASToken(affiliate);
            if (etoken.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                return etoken;

            var mAffiliate = await _context.MainAffiliates.FirstOrDefaultAsync(m => m.Id == affiliate);
            if (mAffiliate == null) return "error: no affiliate found.";

            var response = await PostToApiAsync(
                "/api/SAS/GetExpiration",
                $"{{ \"token\": \"{etoken}\", \"uri\": \"{mAffiliate.Uri}\", \"user\": {userIndex} }}");

            return response.Content ?? "no_result";
        }

        public async Task<Response> SASGetUserDataByIndex(int affiliate, string uri, decimal userIndex)
        {
            var result = await ExecuteWithUnauthorizedRetryAsync(
                () => SASToken(affiliate),
                "/api/SAS/GetUserDataByIndex",
                token => $"{{ \"token\": \"{token}\", \"user\": {(int)userIndex}, \"uri\": \"{uri}\" }}");

            if (!result.error)
                return new Response { error = false, data = result.data };

            return result;
        }

        public async Task<Response> SASGetUserDataByUsername(int affiliate, string uri, string username)
        {
            var result = await ExecuteWithUnauthorizedRetryAsync(
                () => SASToken(affiliate),
                "/api/SAS/GetUserDataByUsername",
                token => $"{{ \"token\": \"{token}\", \"user\": \"{username}\", \"uri\": \"{uri}\" }}");

            if (!result.error)
                return new Response { error = false, data = result.data };

            return result;
        }

        public async Task<Response> SASRefillUser(DtoSASActiveViaDeposit form, int affiliate)
        {
            var etoken = await SASToken(affiliate);
            if (etoken.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                return new Response
                {
                    error = true,
                    message = etoken.Length > 6 ? etoken[6..] : etoken
                };

            int usIndex = (int)form.userIndex;
            int cost = (int)form.cost;
            int receive = (int)form.received;

            var response = await PostToApiAsync(
                "/api/SAS/ActivateUserWithDeposit",
                $"{{ \"token\": \"{etoken}\", " +
                $"\"uri\": \"{form.uri}\", " +
                $"\"connectionString\": \"{form.connectionString}\", " +
                $"\"userIndex\": {usIndex}, " +
                $"\"agent\": {form.agent}, " +
                $"\"userId\": {form.userId}, " +
                $"\"username\": \"{form.username}\", " +
                $"\"emp\": {form.emp}, " +
                $"\"details\": \"{form.details}\", " +
                $"\"profileId\": {form.profileId}, " +
                $"\"subAffiliate\": {form.subAffiliate}, " +
                $"\"saleType\": {BoolJson(form.saleType)}, " +
                $"\"cost\": {cost}, " +
                $"\"received\": {receive}, " +
                $"\"cashAccount\": {form.cashAccount}, " +
                $"\"saveInvoice\": {BoolJson(form.saveInvoice)} }}");

            var stringResult = response.Content;
            if (stringResult == null)
                return ErrorResult("لا يوجد رد من الخادم. الرجاء مراجعة الصفحة في حال تم التفعيل بدون حفظ أو لا");

            try
            {
                return JsonConvert.DeserializeObject<Response>(stringResult)
                    ?? ErrorResult("حدث خطأ أثناء قراءة الرد من الخادم. الرجاء مراجعة الصفحة في حال تم التفعيل أو لا");
            }
            catch
            {
                return ErrorResult("حدث خطأ أثناء قراءة الرد من الخادم. الرجاء مراجعة الصفحة في حال تم التفعيل أو لا");
            }
        }

        #endregion



        #region ftth

        public async Task<string> FTTHToken(int affiliate)
        {
            var refToken = await _context.RefreshTokens.FirstOrDefaultAsync(m => m.Affiliate == affiliate);
            if (refToken == null)
            {
                refToken = new RefreshTokens
                {
                    Affiliate = affiliate,
                    AccessToken = "-",
                    AccessTokenExpire = new DateTime(2000, 1, 1),
                    RefreshToken = "-",
                    RefreshTokenExpire = new DateTime(2000, 1, 1)
                };
                await _context.RefreshTokens.AddAsync(refToken);
                await _context.SaveChangesAsync();
            }

            if (refToken.AccessTokenExpire > DateTime.Now)
                return refToken.AccessToken;

            if (refToken.RefreshTokenExpire > DateTime.Now)
                return await RefreshFtthAccessTokenAsync(refToken);

            return await RequestNewFtthTokenAsync(refToken, affiliate);
        }

        private async Task<string> RefreshFtthAccessTokenAsync(RefreshTokens refToken)
        {
            var response = await PostToApiAsync(
                "/api/NewFTTH/GetFTTHAccessTokenFromRefreshToken",
                $"{{ \"token\": \"{refToken.RefreshToken}\" }}");

            return await ApplyFtthTokenResponseAsync(refToken, response.Content);
        }

        private async Task<string> RequestNewFtthTokenAsync(RefreshTokens refToken, int affiliate)
        {
            var mAffiliate = await _context.MainAffiliates.FirstOrDefaultAsync(m => m.Id == affiliate);
            if (mAffiliate == null) return "error: no affiliate found.";

            var response = await PostToApiAsync(
                "/api/NewFTTH/GetFTTHToken",
                $"{{ \"username\": \"{mAffiliate.Username}\", \"password\": \"{mAffiliate.Password}\" }}");

            return await ApplyFtthTokenResponseAsync(refToken, response.Content);
        }

        private async Task<string> ApplyFtthTokenResponseAsync(RefreshTokens refToken, string? stringResult)
        {
            if (stringResult == null) return "error: an error occurred.";
            if (stringResult.StartsWith("error", StringComparison.OrdinalIgnoreCase)) return stringResult;

            try
            {
                var result = JsonConvert.DeserializeObject<Response>(stringResult);
                if (result == null || result.error)
                    return result?.message ?? "error: an error occurred.";

                var tknData = JsonConvert.DeserializeObject<DtoFtthTokenServerResponse>(result.data?.ToString() ?? "");
                if (tknData == null)
                    return "x. an error while deserialze token object.";

                refToken.AccessToken = tknData.accessToken;
                refToken.AccessTokenExpire = tknData.accessTokenExp;
                refToken.RefreshToken = tknData.refreshToken;
                refToken.RefreshTokenExpire = tknData.refreshTokenExp;
                await _context.SaveChangesAsync();

                return tknData.accessToken;
            }
            catch
            {
                return "error: an error while deserializing object.";
            }
        }

        private async Task RemoveCurrentToken(int affiliate)
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(m => m.Affiliate == affiliate);
            if (token != null)
            {
                token.RefreshTokenExpire = new DateTime(2000, 03, 03);
                token.AccessTokenExpire = new DateTime(2000, 03, 03);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<Response> ExecuteFtthRequestAsync(
            int affiliate,
            string path,
            string jsonBody)
        {
            var etoken = await FTTHToken(affiliate);
            if (etoken.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                return new Response
                {
                    error = true,
                    message = etoken.Length > 6 ? etoken[6..] : etoken
                };

            var response = await PostToApiAsync(path, jsonBody.Replace("{token}", etoken));
            return await ParseFtthResponseAsync(response.Content, affiliate);
        }

        public Task<Response> FTTHGetExpiration(decimal userIndex, int affiliate) =>
            ExecuteFtthRequestAsync(
                affiliate,
                "/api/NewFTTH/GetExpiration",
                $"{{ \"token\": \"{{token}}\", \"user\": {userIndex} }}");

        public async Task<Response> FTTHGetUserDataByIndex(int affiliate, decimal userIndex, int fullData)
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var token = await FTTHToken(affiliate);
                if (token.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                    return ErrorResult(token);

                var response = await PostToApiAsync(
                    "/api/NewFTTH/GetUserDataByIndex",
                    $"{{ \"token\": \"{token}\", \"user\": {userIndex}, \"fullData\": {fullData} }}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (attempt == 1)
                        return ErrorResult("unauthorized");
                    continue;
                }

                if (response.Content == null)
                    return ErrorResult("error: an error occurred.");

                try
                {
                    var result = JsonConvert.DeserializeObject<Response>(response.Content);
                    if (result == null)
                        return ErrorResult("error: an error occurred.");

                    if (result.error)
                    {
                        if (result.message?.Contains("authorize", StringComparison.OrdinalIgnoreCase) == true)
                            await RemoveCurrentToken(affiliate);
                        return result;
                    }

                    return new Response { error = false, data = result.data };
                }
                catch (Exception ex)
                {
                    return ErrorResult(ex.Message);
                }
            }

            return ErrorResult("unauthorized");
        }

        public Task<Response> FTTHRefillSechedulledUser(DtoFTTHShechulledUser form, int affiliate) =>
            ExecuteFtthRequestAsync(
                affiliate,
                "/api/NewFTTH/ChangeAccountScheduller",
                $"{{ \"token\": \"{{token}}\", " +
                $"\"subscriptionId\": \"{form.subscriptionId}\"," +
                $"\"cost\": {form.cost}," +
                $"\"agent\": {form.agent}," +
                $"\"userIndex\": {form.userIndex}," +
                $"\"changeType\": {form.changeType}, " +
                $"\"username\": \"{form.username}\", " +
                $"\"userId\": {form.userId}, " +
                $"\"connectionString\": \"{form.connectionString}\", " +
                $"\"profileId\": {form.profileId}, " +
                $"\"subAffiliate\": {form.subAffiliate}, " +
                $"\"cashAccount\": {form.cashAccount}, " +
                $"\"saleType\": {BoolJson(form.saleType)}, " +
                $"\"received\": {form.received}, " +
                $"\"emp\": {form.emp}, " +
                $"\"details\": \"{form.details}\", " +
                $"\"saveInvoice\": {BoolJson(form.saveInvoice)} }}");

        public Task<Response> FTTHRefillUser(DtoCheckFtthActivateUser form, int affiliate) =>
            ExecuteFtthRequestAsync(
                affiliate,
                "/api/NewFTTH/RefillUser",
                $"{{ \"token\": \"{{token}}\", " +
                $"\"userIndex\": {(int)form.userIndex}," +
                $"\"username\": \"{form.username}\", " +
                $"\"agent\": {form.agent}, " +
                $"\"userId\": {form.userId}, " +
                $"\"subscriptionId\": \"{form.subscriptionId}\"," +
                $"\"connectionString\": \"{form.connectionString}\", " +
                $"\"profileId\": {form.profileId}, " +
                $"\"subAffiliate\": {form.subAffiliate}, " +
                $"\"cashAccount\": {form.cashAccount}, " +
                $"\"saleType\": {BoolJson(form.saleType)}, " +
                $"\"received\": {(int)form.received}, " +
                $"\"cost\": {(int)form.cost}, " +
                $"\"emp\": {form.emp}, " +
                $"\"details\": \"{form.details}\", " +
                $"\"saveInvoice\": {BoolJson(form.saveInvoice)} }}");


        #endregion





        public decimal FormatNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
                return 0;

            var numb = number.Trim();
            if (numb.StartsWith("00")) numb = numb[2..];
            if (numb.StartsWith('0')) numb = numb[1..];
            if (numb.StartsWith('+')) numb = numb[1..];
            if (numb.StartsWith("964")) numb = numb[3..];

            try
            {
                if (numb.Length == 10)
                    numb = "964" + numb;

                var dest = Convert.ToDecimal(numb);
                if (dest < 9647500000000 || dest > 9647999999999)
                    return 0;

                var localNumber = numb[3..];
                return decimal.TryParse(localNumber, out var mobile) ? mobile : 0;
            }
            catch
            {
                return 0;
            }
        }


        public string FormatNumberString(decimal number)
        {
            if (number == 0) return "";
            return "0" + number.ToString("0");
        }


        public string GetConString()
        {
            if (_cachedConString != null)
                return _cachedConString;

            var conStr = _context.Database.GetConnectionString() ?? string.Empty;
            var trustedIndex = conStr.IndexOf("Trusted_Connection", StringComparison.OrdinalIgnoreCase);
            if (trustedIndex > 0)
                conStr = conStr[..(trustedIndex - 1)].TrimEnd(';', ' ');

            _cachedConString = conStr;
            return conStr;
        }

        public ResponseNoData ErrorDescription(string message)
        {
            return new ResponseNoData
            {
                error = true,
                message = message
            };
        }

        public ResponseNoData SuccessDescription(string message)
        {
            return new ResponseNoData
            {
                error = false,
                message = message
            };
        }

        public Response ErrorResponse(string message)
        {
            return new Response
            {
                error = true,
                message = message
            };
        }

        public Response SuccessResponse(object data)
        {
            return new Response
            {
                error = false,
                data = data
            };
        }

    }
}
