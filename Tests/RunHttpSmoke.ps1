param([string]$BaseUrl = 'http://127.0.0.1:5187', [string]$OutputPath = 'docs\TEST_RUN_HTTP.json')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $false
$handler.CookieContainer = New-Object System.Net.CookieContainer
$client = New-Object System.Net.Http.HttpClient $handler
$client.Timeout = [TimeSpan]::FromSeconds(40)
$results = New-Object System.Collections.Generic.List[object]
function Probe([string]$Id, [string]$Path, [int]$Expected, [string]$Method = 'GET', [string]$Body = '', [string]$Contains = '') {
    $request = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::new($Method)), ($BaseUrl + $Path)
    if ($Method -eq 'POST') { $request.Content = New-Object System.Net.Http.StringContent $Body, ([System.Text.Encoding]::UTF8), 'application/x-www-form-urlencoded' }
    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $status = [int]$response.StatusCode
        $location = if ($response.Headers.Location) { $response.Headers.Location.ToString() } else { '' }
        $ok = $status -eq $Expected -and (!$Contains -or $content.Contains($Contains))
        if ($Expected -eq 302) { $ok = $ok -and $location.Contains('/Identity/Account/Login') }
        $results.Add([pscustomobject]@{Id=$Id;Method=$Method;Path=$Path;Expected=$Expected;Actual=$status;Location=$location;Pass=$ok})
        Write-Output "$Id $Method $Path expected=$Expected actual=$status pass=$ok"
        $script:LastBody = $content
        $response.Dispose()
    } finally { $request.Dispose() }
}
try {
    Probe 'AUTH-01-home' '/' 200 'GET' '' 'SmartGym'
    Probe 'AUTH-01-privacy' '/Home/Privacy' 200 'GET' '' 'Privacy'
    Probe 'LOGIN-form' '/Identity/Account/Login' 200 'GET' '' '__RequestVerificationToken'
    $tokenMatch = [regex]::Match($script:LastBody, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (!$tokenMatch.Success) { throw 'Login form did not contain an anti-forgery token.' }
    $token = [System.Net.WebUtility]::HtmlDecode($tokenMatch.Groups[1].Value)
    $encodedToken = [uri]::EscapeDataString($token)
    Probe 'MEM-10-get' '/Identity/Account/Register' 404
    Probe 'MEM-10-post' '/Identity/Account/Register' 404 'POST' "__RequestVerificationToken=$encodedToken"
    Probe 'AUTH-08-missing' '/AccountSetup' 400 'POST' 'UserId=invalid&Token=invalid&Password=invalid'
    Probe 'AUTH-08-invalid' '/AccountSetup' 400 'POST' '__RequestVerificationToken=invalid'
    Probe 'SETUP-invalid-form' '/AccountSetup' 200 'POST' "__RequestVerificationToken=$encodedToken" 'validation-summary-errors'
    $adminPaths = @('Reports','Members','Trainers','Rooms','MembershipPackages','Memberships','ClassSchedules','Bookings','Attendances')
    foreach ($controller in $adminPaths) { Probe "AUTH-05-admin-$controller" "/Admin/$controller" 302 }
    $memberPaths = @('MemberDashboard','MemberProfile','MemberNotifications','MemberBMI','MemberChatbot','MemberBookings/AvailableClasses','MemberBookings/MyBookings')
    foreach ($controller in $memberPaths) { Probe "AUTH-05-member-$controller" "/$controller" 302 }
    Probe 'AUTH-05-export' '/Admin/Reports/ExportAllReports' 302
    Probe 'AUTH-05-book-post' '/MemberBookings/Book' 302 'POST' 'classId=1'
    Probe 'AUTH-05-clear-post' '/MemberChatbot/ClearHistory' 302 'POST' ''
    Probe 'AUTH-05-status-post' '/Admin/Reports/UpdateStatuses' 302 'POST' ''
    Probe 'NOTFOUND' '/NoSuchControllerForTest' 404
    Probe 'STATIC-css' '/css/site.css' 200
    Probe 'STATIC-js' '/js/site.js' 200
    # This tests account rate limiting after all other account requests.
    $statuses = @()
    for ($i=0; $i -lt 35; $i++) {
        $response = $client.GetAsync($BaseUrl + '/AccountSetup?userId=test&token=test').GetAwaiter().GetResult()
        $statuses += [int]$response.StatusCode
        $response.Dispose()
    }
    $results.Add([pscustomobject]@{Id='AUTH-10-rate';Method='GET x35';Path='/AccountSetup';Expected=429;Actual=($statuses -join ',');Location='';Pass=($statuses -contains 429 -and !($statuses -contains 500))})
    ConvertTo-Json -InputObject $results.ToArray() -Depth 4 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
    $failed = @($results | Where-Object { !$_.Pass })
    Write-Output "HTTP checks: $($results.Count); passed: $($results.Count-$failed.Count); failed: $($failed.Count)"
    if ($failed.Count -gt 0) { exit 1 }
} finally { $client.Dispose(); $handler.Dispose() }
