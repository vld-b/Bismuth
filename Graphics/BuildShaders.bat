call "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\VsDevCmd.bat"

set INCLUDEPATH="%WindowsSdkDir%\Include\%WindowsSDKVersion%\um"

fxc SearchNotesBackground.hlsl /nologo /T lib_4_0 /D D2D_FUNCTION /D D2D_ENTRY=main /Fl SearchNotesBackground.fxlib /I %INCLUDEPATH%
fxc SearchNotesBackground.hlsl /nologo /T ps_4_0 /D D2D_FULL_SHADER /D D2D_ENTRY=main /E main /setprivate SearchNotesBackground.fxlib /Fo:SearchNotesBackground.bin /I %INCLUDEPATH%