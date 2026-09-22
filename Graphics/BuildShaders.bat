:: 1. Check if VSINSTALLDIR or VSAPPIDDIR is already set by MSBuild/VS
if defined VSINSTALLDIR (
    call "%VSINSTALLDIR%\Common7\Tools\VsDevCmd.bat"
) else if defined VSAPPIDDIR (
    call "%VSAPPIDDIR%..\..\Common7\Tools\VsDevCmd.bat"
) else (
    :: 2. Fallback: Use vswhere.exe (pre-installed on VS and GitHub Runners) to dynamically locate VS!
    set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
    if exist "!VSWHERE!" (
        for /f "usebackq tokens=*" %%i in (`"!VSWHERE!" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do (
            call "%%i\Common7\Tools\VsDevCmd.bat"
        )
    )
)

set INCLUDEPATH="%WindowsSdkDir%\Include\%WindowsSDKVersion%\um"

set TARGET_ARCH=%~1

set FXC_PATH="%WindowsSdkDir%\bin\%WindowsSDKVersion%\%~1\fxc.exe"

echo "%WindowsSdkDir%\bin\%WindowsSDKVersion%\%~1\fxc.exe"

%FXC_PATH% SearchNotesBackground.hlsl /nologo /T lib_4_0 /D D2D_FUNCTION /D D2D_ENTRY=main /Fl SearchNotesBackground.fxlib /I %INCLUDEPATH%
%FXC_PATH% SearchNotesBackground.hlsl /nologo /T ps_4_0 /D D2D_FULL_SHADER /D D2D_ENTRY=main /E main /setprivate SearchNotesBackground.fxlib /Fo:SearchNotesBackground.bin /I %INCLUDEPATH%