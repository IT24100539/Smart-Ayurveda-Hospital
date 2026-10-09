<#
.SYNOPSIS
    Demonstrates Role-Based Access Control (RBAC) and Protected Operations
    for the Feedback & Communication module (Member 4).

.DESCRIPTION
    Tests public, patient, doctor, and admin roles against feedback & complaint
    endpoints, verifying 200/201 (Allowed), 403 (Forbidden), and 401 (Unauthorized).

.PARAMETER BaseUrl
    Base URL of the Hospital API. Defaults to https://localhost:7443.
#>
param(
    [string]$BaseUrl = "https://localhost:7443"
)

$ErrorActionPreference = "Continue"

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "   SMART AYURVEDA HOSPITAL - FEEDBACK MODULE RBAC DEMONSTRATION          " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "Target API: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Invoke-ApiEndpoint {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Token = $null,
        [object]$Body = $null,
        [int]$ExpectedStatus,
        [string]$RoleName,
        [string]$Description
    )

    $url = "$BaseUrl$Path"
    $headers = @{}
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }

    $statusCode = 0
    $bodyJson = if ($Body) { $Body | ConvertTo-Json -Depth 5 } else { $null }

    try {
        $params = @{
            Uri         = $url
            Method      = $Method
            Headers     = $headers
            ContentType = "application/json"
            TimeoutSec  = 10
        }
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $params["SkipCertificateCheck"] = $true
        }

        if ($bodyJson) {
            $params["Body"] = $bodyJson
        }

        $res = Invoke-WebRequest @params
        $statusCode = [int]$res.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        } else {
            $statusCode = 999
        }
    }

    $passed = ($statusCode -eq $ExpectedStatus)
    $resultTag = if ($passed) { "PASS" } else { "FAIL" }
    $color = if ($passed) { "Green" } else { "Red" }

    Write-Host ("[{0}] {1,-15} {2,-6} {3,-32} -> Got: {4} (Expected: {5}) - {6}" -f `
        $resultTag, $RoleName, $Method, $Path, $statusCode, $ExpectedStatus, $Description) -ForegroundColor $color

    $results.Add([PSCustomObject]@{
        Role        = $RoleName
        Method      = $Method
        Path        = $Path
        Expected    = $ExpectedStatus
        Actual      = $statusCode
        Passed      = $passed
        Description = $Description
    })
}

function Get-UserToken {
    param([string]$Email, [string]$Password)

    $loginBody = @{ email = $Email; password = $Password } | ConvertTo-Json
    try {
        $params = @{
            Uri         = "$BaseUrl/api/auth/login"
            Method      = "Post"
            ContentType = "application/json"
            Body        = $loginBody
            TimeoutSec  = 10
        }
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $params["SkipCertificateCheck"] = $true
        }
        $res = Invoke-RestMethod @params
        return $res.token
    }
    catch {
        Write-Host "Warning: Could not login as $Email. API might be offline." -ForegroundColor Red
        return $null
    }
}

# 1. ANONYMOUS / UNAUTHENTICATED TESTS
Write-Host "--- 1. Testing Unauthenticated (Public / Anonymous) ---" -ForegroundColor White
Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback" -ExpectedStatus 200 `
    -RoleName "Anonymous" -Description "Public testimonials feed (AllowAnonymous)"

Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/staff" -ExpectedStatus 401 `
    -RoleName "Anonymous" -Description "Staff queue requires auth (Unauthorized)"

Invoke-ApiEndpoint -Method "POST" -Path "/api/feedback" -ExpectedStatus 401 -Body @{ rating = 5; comment = "Test" } `
    -RoleName "Anonymous" -Description "Create feedback requires login (Unauthorized)"

# 2. PATIENT ROLE TESTS
Write-Host "`n--- 2. Testing Patient Role (meera.nair@example.local) ---" -ForegroundColor White
$patientToken = Get-UserToken -Email "meera.nair@example.local" -Password "ChangeMe!Patient1"

if ($patientToken) {
    Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/mine" -Token $patientToken -ExpectedStatus 200 `
        -RoleName "Patient" -Description "Patient views own feedback history (Allowed)"

    Invoke-ApiEndpoint -Method "GET" -Path "/api/complaints/me" -Token $patientToken -ExpectedStatus 200 `
        -RoleName "Patient" -Description "Patient views own complaints (Allowed)"

    # Forbidden Staff Operations for Patient
    Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/staff" -Token $patientToken -ExpectedStatus 403 `
        -RoleName "Patient" -Description "Cannot access staff feedback queue (Forbidden)"

    Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/summary" -Token $patientToken -ExpectedStatus 403 `
        -RoleName "Patient" -Description "Cannot view staff feedback stats (Forbidden)"

    Invoke-ApiEndpoint -Method "GET" -Path "/api/complaints" -Token $patientToken -ExpectedStatus 403 `
        -RoleName "Patient" -Description "Cannot view all hospital complaints (Forbidden)"

    Invoke-ApiEndpoint -Method "PATCH" -Path "/api/feedback/00000000-0000-0000-0000-000000000000/moderate" -Token $patientToken -ExpectedStatus 403 `
        -Body @{ action = "Show" } -RoleName "Patient" -Description "Cannot moderate/hide reviews (Forbidden)"
}

# 3. DOCTOR ROLE TESTS
Write-Host "`n--- 3. Testing Doctor Role (doctor@smartayurveda.local) ---" -ForegroundColor White
$doctorToken = Get-UserToken -Email "doctor@smartayurveda.local" -Password "ChangeMe!Doctor1"

if ($doctorToken) {
    Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/summary" -Token $doctorToken -ExpectedStatus 200 `
        -RoleName "Doctor" -Description "Doctor views hospital feedback metrics (Allowed)"

    Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/staff" -Token $doctorToken -ExpectedStatus 200 `
        -RoleName "Doctor" -Description "Doctor views feedback queue (Allowed)"

    Invoke-ApiEndpoint -Method "GET" -Path "/api/complaints" -Token $doctorToken -ExpectedStatus 200 `
        -RoleName "Doctor" -Description "Doctor views complaint queue (Allowed)"

    Invoke-ApiEndpoint -Method "GET" -Path "/api/complaints/assignees" -Token $doctorToken -ExpectedStatus 200 `
        -RoleName "Doctor" -Description "Doctor views complaint assignees (Allowed)"

    # Forbidden Patient Operations for Doctor
    Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/mine" -Token $doctorToken -ExpectedStatus 403 `
        -RoleName "Doctor" -Description "Doctor cannot access personal patient feedback (Forbidden)"

    Invoke-ApiEndpoint -Method "GET" -Path "/api/complaints/me" -Token $doctorToken -ExpectedStatus 403 `
        -RoleName "Doctor" -Description "Doctor cannot access personal patient complaints (Forbidden)"

    Invoke-ApiEndpoint -Method "POST" -Path "/api/feedback" -Token $doctorToken -ExpectedStatus 403 `
        -Body @{ rating = 5; comment = "Staff submission" } -RoleName "Doctor" -Description "Doctor cannot post patient feedback (Forbidden)"
}

# 4. ADMIN ROLE TESTS
Write-Host "`n--- 4. Testing Admin Role (admin@smartayurveda.local) ---" -ForegroundColor White
$adminToken = Get-UserToken -Email "admin@smartayurveda.local" -Password "ChangeMe!Admin1"

if ($adminToken) {
    Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/summary" -Token $adminToken -ExpectedStatus 200 `
        -RoleName "Admin" -Description "Admin views feedback metrics (Allowed)"

    Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/staff" -Token $adminToken -ExpectedStatus 200 `
        -RoleName "Admin" -Description "Admin views moderation queue (Allowed)"

    Invoke-ApiEndpoint -Method "GET" -Path "/api/complaints" -Token $adminToken -ExpectedStatus 200 `
        -RoleName "Admin" -Description "Admin views complaint queue (Allowed)"

    # Forbidden Patient Operations for Admin
    Invoke-ApiEndpoint -Method "GET" -Path "/api/feedback/mine" -Token $adminToken -ExpectedStatus 403 `
        -RoleName "Admin" -Description "Admin cannot access patient-only personal feed (Forbidden)"
}

Write-Host "`n==========================================================================" -ForegroundColor Cyan
$passedCount = ($results | Where-Object { $_.Passed }).Count
$totalCount = $results.Count
Write-Host ("   DEMO SUMMARY: {0}/{1} TESTS PASSED" -f $passedCount, $totalCount) -ForegroundColor Green
Write-Host "==========================================================================" -ForegroundColor Cyan
