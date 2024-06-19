; <COMPILER: v1.1.30.00>
#SingleInstance Force
#NoEnv
#Persistent
#NoTrayIcon

CoordMode, Pixel, Screen


; Function to verify the identity of the caller
CallerIsTrustedService() {
    ; Check if the correct token is provided
    if (A_Args.Length() > 0 && A_Args[1] = "a13e07c188ba1fd889773096e0bfeccf") {
        ; Check for a unique registry value indicating a trusted launch
        RegRead, trustedLaunch, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\ribbon7, TrustedLauncher
		;MsgBox, %trustedLaunch% ; Debugging: Show the value of trustedLaunch
        if (trustedLaunch = "true") {
            ;MsgBox, The script was launched by a trusted source.
			RegWrite, REG_SZ, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\ribbon7, TrustedLauncher, false
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



	
b64Decode(string)
{
    if !(DllCall("crypt32\CryptStringToBinary", "ptr", &string, "uint", 0, "uint", 0x1, "ptr", 0, "uint*", size, "ptr", 0, "ptr", 0))
        throw Exception("CryptStringToBinary failed", -1)
    VarSetCapacity(buf, size, 0)
    if !(DllCall("crypt32\CryptStringToBinary", "ptr", &string, "uint", 0, "uint", 0x1, "ptr", &buf, "uint*", size, "ptr", 0, "ptr", 0))
        throw Exception("CryptStringToBinary failed", -1)
    return StrGet(&buf, size, "UTF-8")
}


; Initialize variables to store the settings
DisableRibbon := "0"

IfExist, %SystemDrive%\ProgramData\Origami IT Lab\VShield\GxPService\AHK\Configuration.enc
{
    SettingsFile = %SystemDrive%\ProgramData\Origami IT Lab\VShield\GxPService\AHK\Configuration.enc
    
    ; Read the base64-encoded content from the file
    FileRead, encodedContent, %SettingsFile%
    
    ; Decode the base64-encoded content
    decodedContent := b64Decode(encodedContent)
    
    ; Split the INI content into an array of lines
    lines := StrSplit(decodedContent, "`r`n")


    ; Loop through each line of the INI content
    for each, line in lines
    {
        ; Debug: Show the raw line content
        ;MsgBox, Line: %line%
        
        ; Check if the line is not empty and contains an equal sign
        if (line != "" && InStr(line, "="))
        {
            ; Split the line into key-value pairs
            keyValue := StrSplit(line, "=")
            
            ; Trim whitespace from the key and value
            key := Trim(keyValue[1])
            value := Trim(keyValue[2])
            
            ; Remove any carriage return or newline characters from the key
            key := StrReplace(key, "`r", "")
            key := StrReplace(key, "`n", "")
            
            ; Debug: Show the cleaned key and value
            ;MsgBox, Cleaned Key: %key%`nValue: %value%
            
            ; Store the value based on the key using if statements
            if (key == "DisableRibbonControl") {
                DisableRibbon := value
			}
        }
    }
        
}



	haha:
GroupAdd, EXC, ahk_class CabinetWClass
GroupAdd, EXC, ahk_class WorkerW
GroupAdd, EXC, ahk_class Progman
GroupAdd, EXC, ahk_class #32770
SetTimer, Update, 50
return

Update:

	GuiControlGet, 1
	CoordMode, Mouse, Screen
	MouseGetPos, msX, msY, msWin, msCtrl
	actWin := WinExist("A")

	curWin := msWin
	curCtrl := msCtrl
	WinExist("ahk_id " curWin)
	WinGetClass, t2

	#If MouseIsOverOpenWindow()
		LButton::return
	#If


return



MouseIsOverOpenWindow()
{
	global t2
	global curCtrl

	if ((t2="#32770" && curCtrl="DirectUIHWND1") and DisableRibbon=1)
	{
		return 1
	}
	else if ((t2 = "CabinetWClass" && curCtrl="DirectUIHWND2") and DisableRibbon=1)
	{
		return 1
	}
	else
	{
		return 0
	}
}




~*Ctrl up::
~*Shift up::
SetTimer, Update, On
return




