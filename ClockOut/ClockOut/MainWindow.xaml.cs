using System.Windows;
using System.Windows.Controls;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Linq;
using System.IO.Compression;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ClockOut
{
    public partial class MainWindow : Window
    {
        private double remainSecond;
        private DispatcherTimer timer; // Declare the timer at the class level

        public MainWindow()
        {
            InitializeComponent();
            Init().Wait();

            leaveWorkButton.Click += LeaveWorkButton_Click;
            turnOffMonitorButton.Click += TurnOffMonitorButton_Click;
        }

        private static string gossoCookie;

        private static async Task Init()
        {
            using StringContent jsonContent = new(JsonSerializer.Serialize(new
            {
                username = "",
                password = "",
                captcha = "",
                returnUrl = "",
            }), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "api/login");
            request.Content = jsonContent;

            // 헤더 추가 (login_headers)
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ko-KR"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ko", 0.9));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.8));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.7));
            request.Headers.Connection.Add("keep-alive");
            request.Content.Headers.ContentLength = 73;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Add("DNT", "1");
            request.Headers.Host = "gw.pixoneer.co.kr";
            request.Headers.Add("Origin", "https://gw.pixoneer.co.kr");
            request.Headers.Referrer = new Uri("https://gw.pixoneer.co.kr/login");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "same-origin");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("sec-ch-ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");

            using HttpResponseMessage loginResponse = await sharedCliend.SendAsync(request).ConfigureAwait(false);

            // GOSSOcookie 추출
            IEnumerable<string> cookies;
            if (loginResponse.Headers.TryGetValues("Set-Cookie", out cookies))
            {
                // 마지막 GOSSOcookie 값을 추출
                string lastGossoCookie = cookies.LastOrDefault(c => c.StartsWith("GOSSOcookie="));
                if (lastGossoCookie != null)
                {
                    string[] cookieParts = lastGossoCookie.Split(';');
                    gossoCookie = cookieParts[0].Split('=')[1];
                }
            }
        }

        private static async Task ClockOut()
        {
            // Get current UTC time and format it
            DateTime nowUtc = DateTime.UtcNow;
            string formattedNowUtc = nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            // Set KST timezone
            TimeZoneInfo kstZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            DateTime nowKst = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, kstZone);
            string workingDay = nowKst.ToString("yyyy-MM-dd");

            using StringContent jsonContent = new(JsonSerializer.Serialize(new
            {
                checkTime = formattedNowUtc,
                timeLineStatus = "{}",
                isNightWork = false,
                workingDay = workingDay,
            }), Encoding.UTF8, "application/json");

            string url = $"api/ehr/timeline/status/clockOut?userId=173&baseDate={workingDay}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = jsonContent;
            //gossoCookie = "ea6efd6f-3597-4c22-9bff-0ab8f9612e6b";
            string cookieHeaderValue = $"userLoginId=22418; userLoginInfoSaved=true; TIMELINE_GUIDE_BADGE_173=done; GOSSOcookie={gossoCookie}; IsCookieActived=true";

            // 헤더 추가
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));
            request.Headers.Add("Cookie", cookieHeaderValue);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ko-KR"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ko", 0.9));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.8));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.7));
            request.Headers.Connection.Add("keep-alive");
            request.Headers.Add("DNT", "1");
            request.Headers.Add("GO-Agent", "");
            request.Headers.Host = "gw.pixoneer.co.kr";
            request.Headers.Add("Origin", "https://gw.pixoneer.co.kr");
            request.Headers.Referrer = new Uri("https://gw.pixoneer.co.kr/app/home");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "same-origin");
            request.Headers.Add("TimeZoneOffset", "540");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("sec-ch-ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");

            using HttpResponseMessage clockInResponse = await sharedCliend.SendAsync(request).ConfigureAwait(false);
            byte[] responseBytes = await clockInResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            using var decompressedStream = new GZipStream(new MemoryStream(responseBytes), CompressionMode.Decompress);
            using var reader = new StreamReader(decompressedStream, Encoding.UTF8);
            var jsonResponse = await reader.ReadToEndAsync();
        }

        private static async Task ClockIn()
        {
            // Get current UTC time and format it
            DateTime nowUtc = DateTime.UtcNow;
            string formattedNowUtc = nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            // Set KST timezone
            TimeZoneInfo kstZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            DateTime nowKst = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, kstZone);
            string workingDay = nowKst.ToString("yyyy-MM-dd");

            using StringContent jsonContent = new(JsonSerializer.Serialize(new
            {
                checkTime = formattedNowUtc,
                timeLineStatus = "{}",
                isNightWork = false,
                workingDay = workingDay,
            }), Encoding.UTF8, "application/json");

            string url = $"api/ehr/timeline/status/clockIn?userId=173&baseDate={workingDay}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = jsonContent;
            //gossoCookie = "ea6efd6f-3597-4c22-9bff-0ab8f9612e6b";
            string cookieHeaderValue = $"userLoginId=22418; userLoginInfoSaved=true; TIMELINE_GUIDE_BADGE_173=done; GOSSOcookie={gossoCookie}; IsCookieActived=true";

            // 헤더 추가
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));
            request.Headers.Add("Cookie", cookieHeaderValue);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ko-KR"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ko", 0.9));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.8));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.7));
            request.Headers.Connection.Add("keep-alive");
            request.Headers.Add("DNT", "1");
            request.Headers.Add("GO-Agent", "");
            request.Headers.Host = "gw.pixoneer.co.kr";
            request.Headers.Add("Origin", "https://gw.pixoneer.co.kr");
            request.Headers.Referrer = new Uri("https://gw.pixoneer.co.kr/app/home");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "same-origin");
            request.Headers.Add("TimeZoneOffset", "540");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("sec-ch-ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");

            using HttpResponseMessage clockInResponse = await sharedCliend.SendAsync(request).ConfigureAwait(false);
            byte[] responseBytes = await clockInResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            using var decompressedStream = new GZipStream(new MemoryStream(responseBytes), CompressionMode.Decompress);
            using var reader = new StreamReader(decompressedStream, Encoding.UTF8);
            var jsonResponse = await reader.ReadToEndAsync();
        }

        private static HttpClient sharedCliend = new()
        {
            BaseAddress = new Uri("https://gw.pixoneer.co.kr/")
        };

        private void LeaveWorkButton_Click(object sender, RoutedEventArgs e)
        {
            string remain = inputTextBox.Text;
            if (remain != "")
            {
                remainSecond = double.Parse(remain) * 60;
                StartCountdown(remainSecond);
            }
        }

        private void StartCountdown(double seconds)
        {
            // Stop the previous timer if it exists
            if (timer != null)
            {
                timer.Stop();
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                if (seconds > 0)
                {
                    seconds--;
                    remainingTimeTextBlock.Text = $"잔여시간(초): {seconds}";
                }
                else
                {
                    timer.Stop();
                }
            };
            timer.Start();
        }

        private void TurnOffMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            SetThreadExecutionState(ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED | ExecutionState.ES_AWAYMODE_REQUIRED);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(ExecutionState esFlags);

        [Flags]
        private enum ExecutionState : uint
        {
            ES_CONTINUOUS = 0x80000000,
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_AWAYMODE_REQUIRED = 0x00000040,
        }

   
    }
}