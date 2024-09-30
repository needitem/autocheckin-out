using ClockOut.Helpers;
using ClockOut.Models;
using DotNetEnv;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;
namespace ClockOut.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly DispatcherTimer timer;
        private double remainSecond;
        private string _remainingTimeText = "잔여시간(초): ";

        // CookieContainer를 사용하여 쿠키를 자동으로 관리
        private static readonly CookieContainer cookieContainer = new CookieContainer();

        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        };

        private static readonly HttpClient sharedClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://gw.pixoneer.co.kr/")
        };

        #endregion

        #region Properties

        public string RemainingTimeText
        {
            get => _remainingTimeText;
            set
            {
                _remainingTimeText = value;
                OnPropertyChanged(nameof(RemainingTimeText));
            }
        }

        private string _inputTime;
        public string InputTime
        {
            get => _inputTime;
            set
            {
                if (_inputTime != value)
                {
                    _inputTime = value;
                    OnPropertyChanged(nameof(InputTime));
                    LeaveWorkCommand.RaiseCanExecuteChanged(); // CanExecute 상태 갱신
                }
            }
        }

        #endregion

        #region Commands

        public RelayCommand LeaveWorkCommand { get; }
        public ICommand TurnOffMonitorCommand { get; }

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            // Load .env file
            Env.Load(); // Added

            LeaveWorkCommand = new RelayCommand(async _ => await LeaveWorkAsync(), _ => CanExecuteLeaveWork());
            TurnOffMonitorCommand = new RelayCommand(_ => TurnOffMonitor());

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;

            // 비동기 초기화 시작
            _ = InitAsync();
        }


        #endregion

        #region Methods

        private async Task InitAsync()
        {
            try
            {
                var credentials = new UserCredentials
                {
                    Username = Environment.GetEnvironmentVariable("USERNAME"),
                    Password = Environment.GetEnvironmentVariable("PASSWORD")
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(new
                {
                    username = credentials.Username,
                    password = credentials.Password,
                    captcha = "",
                    returnUrl = "",
                }), Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "api/login")
                {
                    Content = jsonContent
                };

                // 헤더 추가 (login_headers)
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));

                using HttpResponseMessage response = await sharedClient.SendAsync(request).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    // 필요에 따라 추가적인 쿠키 설정
                    var uri = new Uri("https://gw.pixoneer.co.kr/");
                    cookieContainer.Add(uri, new Cookie("userLoginId", credentials.Username));
                    cookieContainer.Add(uri, new Cookie("userLoginInfoSaved", "true"));
                    cookieContainer.Add(uri, new Cookie("TIMELINE_GUIDE_BADGE_173", "done"));
                    cookieContainer.Add(uri, new Cookie("IsCookieActived", "true"));
                }
                else
                {
                    // 오류 처리 로직 추가
                    MessageBox.Show($"로그인 요청 실패: {response.ReasonPhrase}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"HTTP 요청 오류: {httpEx.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteLeaveWork()
        {
            return IsValidInputTime(InputTime) && !timer.IsEnabled;
        }

        private bool IsValidInputTime(string input)
        {
            if (int.TryParse(input, out int minutes))
            {
                return minutes > 0;
            }
            return false;
        }

        private async Task LeaveWorkAsync()
        {
            try
            {
                // 입력된 시간을 분 단위로 파싱하여 초로 변환
                if (!int.TryParse(InputTime, out int inputMinutes) || inputMinutes <= 0)
                {
                    MessageBox.Show("유효한 시간을 입력해주세요 (1 이상의 숫자).", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                remainSecond = inputMinutes * 60;
                RemainingTimeText = $"잔여시간(초): {remainSecond}";
                timer.Start();

                // CanExecute 상태 갱신
                LeaveWorkCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"퇴근 처리 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task SendTimelineStatusAsync(string action, string workingDay)
        {
            try
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(new
                {
                    checkTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    timeLineStatus = new { }, // 수정된 부분
                    isNightWork = false,
                    workingDay = workingDay,
                }), Encoding.UTF8, "application/json");

                string url = $"api/ehr/timeline/status/{action}?userId=173&baseDate={workingDay}";

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = jsonContent
                };

                // 헤더 추가 (사용자 요청과 동일하게 설정)
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));

                // Cookie 설정 제거 (CookieContainer가 자동으로 처리)

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

                using HttpResponseMessage response = await sharedClient.SendAsync(request).ConfigureAwait(false);

                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false); // 추가된 부분

                if (response.IsSuccessStatusCode)
                {
                    byte[] responseBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    using var decompressedStream = new GZipStream(new MemoryStream(responseBytes), CompressionMode.Decompress);
                    using var reader = new StreamReader(decompressedStream, Encoding.UTF8);
                    var jsonResponse = await reader.ReadToEndAsync();
                    // JSON 응답 처리 로직 추가
                }
                else
                {
                    // 오류 처리 로직 추가
                    MessageBox.Show($"요청 실패: {response.ReasonPhrase}\n내용: {responseContent}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"HTTP 요청 오류: {httpEx.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TurnOffMonitor()
        {
            try
            {
                // 현재 활성화된 모든 창에 메시지를 전송하여 모니터를 끕니다.
                SendMessage(new IntPtr(0xFFFF), WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"모니터 끄기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (remainSecond > 0)
            {
                remainSecond--;
                RemainingTimeText = $"잔여시간(초): {remainSecond}";
            }
            else
            {
                timer.Stop();
                RemainingTimeText = "잔여시간(초): 0";
                LeaveWorkCommand.RaiseCanExecuteChanged(); // 버튼 활성화 상태 갱신

                // 퇴근 처리 로직 수행
                DateTime nowKst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"));
                string workingDay = nowKst.ToString("yyyy-MM-dd");

                await SendTimelineStatusAsync("clockOut", workingDay);
            }
        }

        #region WinAPI

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SYSCOMMAND = 0x112;
        private const int SC_MONITORPOWER = 0xF170;

        #endregion

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}