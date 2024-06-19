SetTitleMatchMode, 2
	#SingleInstance Force
	#NoEnv
	#Persistent
	#NoTrayIcon
	#InstallKeybdHook
	#UseHook


; Function to verify the identity of the caller
CallerIsTrustedService() {
    ; Check if the correct token is provided
    if (A_Args.Length() > 0 && A_Args[1] = "a13e07c188ba1fd889773096e0bfeccf") {
        ; Check for a unique registry value indicating a trusted launch
        RegRead, trustedLaunch, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\rns, TrustedLauncher
		;MsgBox, %trustedLaunch% ; Debugging: Show the value of trustedLaunch
        if (trustedLaunch = "true") {
            ;MsgBox, The script was launched by a trusted source.
			RegWrite, REG_SZ, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\rns, TrustedLauncher, false

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
DisableClip := Rename := WindowTitle := "0"

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
            if (key == "Rename") {
                Rename := value
            } else if (key == "WindowTitle") {
                WindowTitle := value
            } 

        }
    }
        
}


	
	
	loop
	{		

		MouseGetPos, , , ID, control
		WinGet, id, ID, A
		WinGetClass, class, ahk_id %id%
		WinGetTitle, Title, A
		WinGet, t3, ProcessName, ahk_id %id%
		
		if((class = "Progman") and Rename=1)
		{
			If(ActiveControlIsOfClass("Edit1"))
			{
				Send {Escape}
			}
		}
		if((class = "CabinetWClass") and Rename=1)
		{
			If(ActiveControlIsOfClass("ModernSearchBox1"))
			{
				Send {Escape}
			}
		}
		if((class="CabinetWClass" ) and Rename=1)
		{
			If(ActiveControlIsOfClass("Edit2"))
			{
				Send {Escape}
			}
			If(ActiveControlIsOfClass("Edit1"))
			{
				Send {Escape}
			}
		}
		if((class="#32770" ) and Title!="Run" and Rename=1)
		{
			If InStrGrp(Title, WindowTitle)
			{
				;do Nothing
			}
			else If(Title="Open" and t3!="Syngistix.exe")
			{
				If(ActiveControlIsOfClass("Edit1"))
				{
					WinActivate
					ControlFocus, DirectUIHWND3, A
					ControlFocus, DirectUIHWND2, A
					ControlFocus, SysListView321, A
				}
			}
			
			else If(Title="Save As"  and t3!="Syngistix.exe")
			{
				If(ActiveControlIsOfClass("Edit2"))
				{
					WinActivate
					ControlFocus, Edit1, A
				}
				If(ActiveControlIsOfClass("Edit1"))
				{
					WinActivate
					WinGet, OutputVar, ControlList, ahk_class #32770
					if OutputVar contains Edit2
				    {
						ControlGet, OutputVar, Line ,1, Edit2, Save As
						if(OutputVar="")
						{
							;Tooltip % OutputVar  		;ControlFocus, Edit2, A			;Sleep, 1000  ; 1 second
						}
						else
						{
							ControlFocus, Edit2, A
						}
				    }
				}
			}
			
			else If(Title="Select Folder"  and t3!="Syngistix.exe")
			{
				If(ActiveControlIsOfClass("Edit2"))
				{
					WinActivate
					ControlFocus, Edit1, A
				}
				If(ActiveControlIsOfClass("Edit1"))
				{
					WinActivate
					WinGet, OutputVar, ControlList, ahk_class #32770
					if OutputVar contains Edit2
				    {
						ControlGet, OutputVar, Line ,1, Edit2, Select Folder
						if(OutputVar="")
						{
							;Tooltip % OutputVar  		;ControlFocus, Edit2, A			;Sleep, 1000  ; 1 second
						}
						else
						{
							ControlFocus, Edit2, A
						}
				    }
				}
			}
			
			
		}
		
		
		sleep 50
	}
	

ActiveControlIsOfClass(Class) {
		ControlGetFocus, FocusedControl, A
		return (FocusedControl=Class)
	}
InStrGrp(haystack, needle)
	{
	   needles := StrSplit(needle, "|")
	   Loop, % needles.Length()
		  if InStr(haystack, needles[A_Index])
			 return true
	   return false
	}

	