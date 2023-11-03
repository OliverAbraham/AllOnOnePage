;--------------------------------------------------------------------------------------------------
; AllOnOnePage installer creation script
; Oliver Abraham, 03.11.2023
;--------------------------------------------------------------------------------------------------

!ifdef HAVE_UPX
!packhdr tmp.dat "upx\upx -9 tmp.dat"
!endif

!ifdef NOCOMPRESS
SetCompress off
!endif

;--------------------------------------------------------------------------------------------------
Name "AllOnOnePage"
Caption "AllOnOnePage Dashboard"
Icon "${NSISDIR}\Contrib\Graphics\Icons\nsis1-install.ico"
OutFile "AllOnOnePage_Setup_english.exe"
SetDateSave on
SetDatablockOptimize on
CRCCheck on
SilentInstall normal
BGGradient 000000 800000 FFFFFF
InstallColors FF8080 000030
XPStyle on
InstallDir "$PROGRAMFILES64\Abraham Beratung\AllOnOnePage"
InstallDirRegKey HKLM "Software\Abraham Beratung\AllOnOnePage" "Install_Dir"
CheckBitmap "${NSISDIR}\Contrib\Graphics\Checks\classic-cross.bmp"
LicenseText "License agreement"
LicenseData "AllOnOnePageLicenseText.txt"
RequestExecutionLevel admin

;--------------------------------------------------------------------------------------------------
Page license
Page directory
Page instfiles
UninstPage uninstConfirm
UninstPage instfiles
AutoCloseWindow false
ShowInstDetails show

;--------------------------------------------------------------------------------------------------
Section "-" ; empty string makes it hidden, so would starting with -
  WriteRegStr HKLM "SOFTWARE\Abraham Beratung\AllOnOnePage" "Install_Dir" "$INSTDIR"
  ; write uninstall strings
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AllOnOnePage" "DisplayName" "AllOnOnePage (remove only)"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AllOnOnePage" "UninstallString" '"$INSTDIR\AllOnOnePage-Uninstall.exe"'

  SetOutPath $INSTDIR
  CreateDirectory "$DOCUMENTS\All on one page"
  File /r ..\AllOnOnePage\bin\publish\*
  WriteUninstaller "AllOnOnePage-Uninstall.exe"
SectionEnd


SectionGroup /e SectionGroup1
Section "Test CreateShortCut"
  SectionIn 1 2 3
  CreateDirectory "$SMPROGRAMS\AllOnOnePage"
  SetOutPath $INSTDIR ; for working directory
  CreateShortCut "$SMPROGRAMS\AllOnOnePage\AllOnOnePage.lnk" "$INSTDIR\AllOnOnePage.exe"
  CreateShortCut "$SMPROGRAMS\AllOnOnePage\Uninstall AllOnOnePage.lnk" "$INSTDIR\AllOnOnePage-Uninstall.exe"
SectionEnd
SectionGroupEnd


;--------------------------------------------------------------------------------------------------
; Uninstaller
;--------------------------------------------------------------------------------------------------
UninstallText "This will uninstall AllOnOnePage. Hit next to continue."
UninstallIcon "${NSISDIR}\Contrib\Graphics\Icons\nsis1-uninstall.ico"

Section "Uninstall"

  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AllOnOnePage"
  DeleteRegKey HKLM "SOFTWARE\Abraham Beratung\AllOnOnePage"
  Delete "$SMPROGRAMS\AllOnOnePage\*.*"
  RMDir  "$SMPROGRAMS\AllOnOnePage"
  RMDir /r $INSTDIR

  IfFileExists "$INSTDIR" 0 NoErrorMsg
    MessageBox MB_OK "Note: $INSTDIR could not be removed!" IDOK 0 ; skipped if file doesn't exist
  NoErrorMsg:

SectionEnd
