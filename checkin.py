import requests
import json
from datetime import datetime, timedelta, timezone

# 1. 로그인 요청
login_url = "https://gw.pixoneer.co.kr/api/login"
login_headers = {
    "Accept": "application/json, text/javascript, */*; q=0.01",
    "Accept-Encoding": "gzip, deflate, br, zstd",
    "Accept-Language": "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7",
    "Connection": "keep-alive",
    "Content-Length": "73",
    "Content-Type": "application/json",
    "DNT": "1",
    "Host": "gw.pixoneer.co.kr",
    "Origin": "https://gw.pixoneer.co.kr",
    "Referer": "https://gw.pixoneer.co.kr/login",
    "Sec-Fetch-Dest": "empty",
    "Sec-Fetch-Mode": "cors",
    "Sec-Fetch-Site": "same-origin",
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
    "X-Requested-With": "XMLHttpRequest",
    "sec-ch-ua": '"Not/A)Brand";v="8", "Chromium";v="126"',
    "sec-ch-ua-mobile": "?0",
    "sec-ch-ua-platform": '"Windows"',
}
login_payload = {
    "username": "",
    "password": "",
    "captcha": "",
    "returnUrl": "",
}

login_response = requests.post(login_url, headers=login_headers, json=login_payload)

now_utc = datetime.now(timezone.utc)
formatted_now_utc = now_utc.strftime("%Y-%m-%dT%H:%M:%S.000Z")
KST = timezone(timedelta(hours=9))
now_kst = datetime.now(KST)
working_day = now_kst.strftime("%Y-%m-%d")


# 2. 로그인 응답에서 쿠키 추출
login_cookies = login_response.cookies
gosso_cookie = login_cookies.get("GOSSOcookie")
print(gosso_cookie)

clock_in_url = f"https://gw.pixoneer.co.kr/api/ehr/timeline/status/clockIn?userId=173&baseDate={working_day}"
clock_in_headers = {
    "Accept": "application/json, text/javascript, */*; q=0.01",
    "Accept-Encoding": "gzip, deflate, br, zstd",
    "Accept-Language": "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7",
    "Connection": "keep-alive",
    "Content-Length": "106",
    "Content-Type": "application/json",
    "Cookie": f"userLoginId=22418; userLoginInfoSaved=true; TIMELINE_GUIDE_BADGE_173=done; GOSSOcookie={gosso_cookie}; IsCookieActived=true",
    "DNT": "1",
    "GO-Agent": "",
    "Host": "gw.pixoneer.co.kr",
    "Origin": "https://gw.pixoneer.co.kr",
    "Referer": "https://gw.pixoneer.co.kr/app/home",
    "Sec-Fetch-Dest": "empty",
    "Sec-Fetch-Mode": "cors",
    "Sec-Fetch-Site": "same-origin",
    "TimeZoneOffset": "540",
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
    "X-Requested-With": "XMLHttpRequest",
    "sec-ch-ua": '"Not/A)Brand";v="8", "Chromium";v="126"',
    "sec-ch-ua-mobile": "?0",
    "sec-ch-ua-platform": '"Windows"',
}
clock_in_payload = {
    "checkTime": formatted_now_utc,
    "timelineStatus": {},
    "isNightWork": False,
    "workingDay": working_day,
}

clock_in_response = requests.post(
    clock_in_url, headers=clock_in_headers, json=clock_in_payload, cookies=login_cookies
)

print(clock_in_response.status_code)
print(clock_in_response.json())

