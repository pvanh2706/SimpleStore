@echo off
setlocal

cd /d "%~dp0"

set "BACKEND_FAILED=0"
set "FRONTEND_FAILED=0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet was not found in PATH.
    set "BACKEND_FAILED=1"
) else (
    echo.
    echo ============================================================
    echo Running backend tests - Release
    echo ============================================================
    dotnet test SimpleStore.sln --configuration Release
    if errorlevel 1 (
        echo [FAILED] Backend tests failed.
        set "BACKEND_FAILED=1"
    ) else (
        echo [PASSED] Backend tests passed.
    )
)

where pnpm >nul 2>&1
if errorlevel 1 (
    echo [ERROR] pnpm was not found in PATH.
    set "FRONTEND_FAILED=1"
) else (
    echo.
    echo ============================================================
    echo Running frontend tests
    echo ============================================================
    call pnpm --dir src/frontend/simplestore-web test
    if errorlevel 1 (
        echo [FAILED] Frontend tests failed.
        set "FRONTEND_FAILED=1"
    ) else (
        echo [PASSED] Frontend tests passed.
    )
)

echo.
echo ============================================================
if "%BACKEND_FAILED%"=="0" if "%FRONTEND_FAILED%"=="0" (
    echo ALL LOCAL TESTS PASSED.
    echo ============================================================
    if /i not "%~1"=="--no-pause" pause
    exit /b 0
)

echo LOCAL TESTS FAILED.
if not "%BACKEND_FAILED%"=="0" echo - Backend: FAILED
if not "%FRONTEND_FAILED%"=="0" echo - Frontend: FAILED
echo ============================================================
if /i not "%~1"=="--no-pause" pause
exit /b 1
