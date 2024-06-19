;
; Window Spy
;

#SingleInstance Force
#NoEnv
#Persistent
#NoTrayIcon


; Function to verify the identity of the caller
CallerIsTrustedService() {
    ; Check if the correct token is provided
    if (A_Args.Length() > 0 && A_Args[1] = "a13e07c188ba1fd889773096e0bfeccf") {
        ; Check for a unique registry value indicating a trusted launch
        RegRead, trustedLaunch, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\qat, TrustedLauncher
		;MsgBox, %trustedLaunch% ; Debugging: Show the value of trustedLaunch
        if (trustedLaunch = "true") {
            ;MsgBox, The script was launched by a trusted source.
			RegWrite, REG_SZ, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\qat, TrustedLauncher, false
            return true
        }
    }
    return false
}

; Check if the correct token is provided
if (!CallerIsTrustedService()) {
    MsgBox, Access denied. This script can only be called from the trusted source.
    ExitApp
}





SetWorkingDir, %A_ScriptDir%
SetBatchLines, -1
CoordMode, Pixel, Screen


Gui, New, hwndhGui AlwaysOnTop Resize MinSize

SetTimer, Update, 100
return


Update:
GuiControlGet, Ctrl_FollowMouse
CoordMode, Mouse, Screen
MouseGetPos, msX, msY, msWin, msCtrl
actWin := WinExist("A")
if Ctrl_FollowMouse
{
	curWin := msWin
	curCtrl := msCtrl
	WinExist("ahk_id " curWin)
}
else
{
	curWin := actWin
	ControlGetFocus, curCtrl
}
WinGetTitle, t1
WinGetClass, t2
WinGet, t3, ProcessName
WinGet, t4, PID


	if (t2="Net UI Tool Window")
	{
				
		if (t3="explorer.exe")
		{
		
			Send, {Esc}
		}
	}


return


