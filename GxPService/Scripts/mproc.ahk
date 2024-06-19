; <COMPILER: v1.1.30.00>
; <COMPILER with ANSI 32 bit>
#SingleInstance Force
#NoEnv
#Persistent
#NoTrayIcon



; Function to verify the identity of the caller
CallerIsTrustedService() {
    ; Check if the correct token is provided
    if (A_Args.Length() > 0 && A_Args[1] = "a13e07c188ba1fd889773096e0bfeccf") {
        ; Check for a unique registry value indicating a trusted launch
        RegRead, trustedLaunch, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\mproc, TrustedLauncher
		;MsgBox, %trustedLaunch% ; Debugging: Show the value of trustedLaunch
        if (trustedLaunch = "true") {
            ;MsgBox, The script was launched by a trusted source.
			RegWrite, REG_SZ, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\mproc, TrustedLauncher, false
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
Cut := EmptyRecycleBin := Copy := Paste := Delete := CopyTo := MoveTo := SendTo := Rename := "0"
OpenWith := CreateShortcut := Burntodisc := Properties := Share := Print := WinKey := "0"


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
            if (key == "Cut") {
                Cut := value
            } else if (key == "Empty Recycle Bin") {
                EmptyRecycleBin := value
            } else if (key == "Copy") {
                Copy := value
            } else if (key == "Paste") {
                Paste := value
            } else if (key == "Delete") {
                Delete := value
            } else if (key == "CopyTo") {
                CopyTo := value
            } else if (key == "MoveTo") {
                MoveTo := value
            } else if (key == "SendTo") {
                SendTo := value
            } else if (key == "Rename") {
                Rename := value
            } else if (key == "OpenWith") {
                OpenWith := value
            } else if (key == "CreateShortcut") {
                CreateShortcut := value
            } else if (key == "BurnToDisc") {
                Burntodisc := value
            } else if (key == "Properties") {
                Properties := value
            } else if (key == "Share") {
                Share := value
            }
        }
    }
        
}

GroupAdd, exclusion, ahk_class CabinetWClass
GroupAdd, exclusion, ahk_class WorkerW
GroupAdd, exclusion, ahk_class Progman
GroupAdd, exclusion, ahk_class #32770
GroupAdd, exclusion, ahk_class TPTFrmOpenDlg2

MF_BYPOSITION := 0x0400, MF_GRAYED := 0x01, MF_ENABLED := 0x00

loop
{
	IfWinActive ahk_group exclusion	
	{
		WinWait, ahk_class #32768
		SendMessage, 0x01E1
		MenuID := ErrorLevel
		Loop, % DllCall("GetMenuItemCount", UInt, MenuID)
		{
			
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Cut"  && Cut=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Empty Recycle Bin"  && EmptyRecycleBin=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Copy" && Copy=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Paste"  && Paste=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Delete" && Delete=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Copy To"  && CopyTo=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Move To"  && MoveTo=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Send To"  && SendTo=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Rename"  && Rename=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Open with..."  && Openwithdot=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Open with"  && OpenWith=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Create Shortcut"  && CreateShortcut=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Burn to disc"  && Burntodisc=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Properties"  && Properties=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Share"  && Share=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)
			if RegExReplace(GetMenuItemText(MenuID, A_Index-1), "&")="Open with..."  && OpenWith=1
			DllCall("EnableMenuItem", "UInt", MenuID, "UInt", A_Index-1, "UInt", MF_BYPOSITION | MF_GRAYED)

		}
	}
	
	
}

GetMenuItemText(ByRef MenuID, i, MenuStrlen=0){
	global MF_BYPOSITION
	VarSetCapacity(MenuStr, MenuStrLen)
	Len := DllCall("GetMenuString", "UInt", MenuID, "UInt", i, "str", MenuStr, "UInt", MenuStrLen, "UInt", MF_BYPOSITION)
	if(MenuStrLen=0 and Len!=0)
		return GetMenuItemText(MenuID, i, Len+1)
	Else
		return MenuStr
}