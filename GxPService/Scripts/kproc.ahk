; <COMPILER: v1.1.30.00>
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
        RegRead, trustedLaunch, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\kproc, TrustedLauncher
		;MsgBox, %trustedLaunch% ; Debugging: Show the value of trustedLaunch
        if (trustedLaunch = "true") {
            ;MsgBox, The script was launched by a trusted source.
			RegWrite, REG_SZ, HKEY_LOCAL_MACHINE, Software\VShield\GxPService\Agents\kproc, TrustedLauncher, false
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
AltF4 := AltTab := Escape := Backspace := TabKey := CtrlKey := AltKey := AltGrKey := FunctionKeys := "0"
MouseMiddleButton := TaskManager := "0"

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
            } else if (key == "Burntodisc") {
                Burntodisc := value
            } else if (key == "Properties") {
                Properties := value
            } else if (key == "Share") {
                Share := value
            } else if (key == "Print") {
                Print := value
            } else if (key == "WinKey") {
                WinKey := value
            } else if (key == "AltF4Shortcut") {
                AltF4 := value
            } else if (key == "AltTabShortcut") {
                AltTab := value
            } else if (key == "EscShortcut") {
                Escape := value
            } else if (key == "BackspaceShortcut") {
                Backspace := value
            } else if (key == "TabShortcut") {
                TabKey := value
            } else if (key == "CtrlShortcut") {
                CtrlKey := value
            } else if (key == "AltShortcut") {
                AltKey := value
            } else if (key == "AltGrShortcut") {
                AltGrKey := value
            } else if (key == "FunctionKeysShortcut") {
                FunctionKeys := value
            } else if (key == "MouseMiddleButton") {
                MouseMiddleButton := value
            } else if (key == "TaskManager") {
                TaskManager := value
            }
        }
    }
        
}

; Display or use the settings as needed
    ;MsgBox, Final Settings:`nCut: %Cut%`nEmpty Recycle Bin: %EmptyRecycleBin%`nCopy: %Copy%`nPaste: %Paste%`nDelete: %Delete%`nCopy To: %CopyTo%`nMove To: %MoveTo%`nSend To: %SendTo%`nRename: %Rename%`nOpen With: %OpenWith%`nCreate Shortcut: %CreateShortcut%`nBurn to disc: %Burntodisc%`nProperties: %Properties%`nShare: %Share%`nPrint: %Print%`nWinKey: %WinKey%`nAltF4: %AltF4%`nAltTab: %AltTab%`nEscape: %Escape%`nBackspace: %Backspace%`nTabKey: %TabKey%`nCtrlKey: %CtrlKey%`nAltKey: %AltKey%`nAltGrKey: %AltGrKey%`nFunctionKeys: %FunctionKeys%`nMouse Middle Button: %MouseMiddleButton%`nTask Manager: %TaskManager%


	loop
	{
		WinGet, id, ID, A
		WinGetClass, class, ahk_id %id%
		WinGetTitle, Title, A
		MouseGetPos, , , id, control

		
		
		GroupAdd, EXC, ahk_class CabinetWClass
		GroupAdd, EXC, ahk_class WorkerW
		GroupAdd, EXC, ahk_class Progman
		GroupAdd, EXC, ahk_class #32770
		GroupAdd, EXC, ahk_class TPTFrmOpenDlg2
		
		#IfWinActive ahk_group EXC
			del::
			+del::
			!del::
			#del::
			RAlt & del::
			RAlt & NumpadDel::
			NumpadDel::			
			#NumpadDel::
			+NumpadDel::
			!NumpadDel::
			^z::			
			^d::
				If(Delete=1)
					MsgBox,262160,Warning, % "Delete operation has been disabled."
				Else
					Send, {del}
			return
			
			PrintScreen::
			+PrintScreen::
			!PrintScreen::
			#PrintScreen::
			^PrintScreen::
			RAlt & PrintScreen::							
				If(Print=1)
						MsgBox,262160,Warning, % "PrintScreen operation has been disabled."
					Else
						Send, {PrintScreen}
				return

			F2::
				If(Rename=1)
					MsgBox,262160,Warning, % "Rename operation has been disabled."
				Else
					Send, {F2}
			return
			
			!Enter::
			!NumpadEnter::
			RAlt & NumpadEnter::
			RAlt & Enter::
				If(Properties=1)
						MsgBox,262160,Warning, % "Properties operation has been disabled."
				return

			
			^x::
				If(Cut=1)
					MsgBox,262160,Warning, % "Cut operation has been disabled."
				Else
					Send, ^{x}
			return
			

			^v::
			+Ins::
				If(Paste=1)
					MsgBox,262160,Warning, % "Paste operation has been disabled."
				Else
					Send, ^{v}
			return
	
			^c::
				If(Copy=1)
					MsgBox,262160,Warning, % "Copy operation has been disabled."
				Else
					Send, ^{c}
			return

			#x::
				If(StartMenu=1)
					MsgBox,262160,Warning, % "Start Menu operation has been disabled."
				Else
					Send, #{x}
			return
			
			#d::
				If(WinKey=1)
					MsgBox,262160,Warning, % "Win + D operation has been disabled."
				Else
					Send, #{d}
			return
			
			#m::
				If(WinKey=1)
					MsgBox,262160,Warning, % "Win + M operation has been disabled."
				Else
					Send, #{m}
			return

			#e::
				If(WinKey=1)
					MsgBox,262160,Warning, % "Win + E operation has been disabled."
				Else
					Send, #{e}
			return
			
			
			
			
			!F4::
            If(AltF4=1)
                MsgBox,262160,Warning, % "Alt + F4 operation has been disabled."
			return

			!Tab::
				If(AltTab=1)
					MsgBox,262160,Warning, % "Alt + Tab operation has been disabled."
			return

			Esc::
				If(Escape=1)
					MsgBox,262160,Warning, % "Escape operation has been disabled."
			return

			Backspace::
				If(Backspace=1)
					MsgBox,262160,Warning, % "Backspace operation has been disabled."
			return

			Tab::
				If(TabKey=1)
					MsgBox,262160,Warning, % "Tab operation has been disabled."
			return

			^::
				If(CtrlKey=1)
					MsgBox,262160,Warning, % "Control key operation has been disabled."
			return

			!::
				If(AltKey=1)
					MsgBox,262160,Warning, % "Alt key operation has been disabled."
			return

			<^>!::
				If(AltGrKey=1)
					MsgBox,262160,Warning, % "Alt Gr key operation has been disabled."
			return

			F1::
			F3::
			F4::
			F5::
			F6::
			F7::
			F8::
			F9::
			F10::
			F11::
			F12::
				If(FunctionKeys=1)
					MsgBox,262160,Warning, % "Function keys operation has been disabled."
			return

			MButton::
				If(MouseMiddleButton=1)
					MsgBox,262160,Warning, % "Mouse middle button operation has been disabled."
			return
		#IfWinActive
		

		
		
		;Window Kill command for Delete
		If WinActive("Confirm File Delete") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
		
		If WinActive("Confirm Multiple File Delete") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinActive("Confirm Folder Delete") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinActive("Delete File") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinActive("Delete Folder") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinActive("Delete Multiple Items") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinActive("Delete") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		;Window Exists command for Delete
		
		If WinExist("Confirm File Delete") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
		
		If WinExist("Confirm Multiple File Delete") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinExist("Confirm Folder Delete") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinExist("Delete File") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinExist("Delete Folder") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinExist("Delete Multiple Items") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}
			
		If WinExist("Delete") && Delete=1
		{
			WinKill
			MsgBox,262160,Warning, % "Delete operation has been disabled."
		}

		
		If WinActive("Move Items") && MoveTo=1
		{
			WinKill
			MsgBox,262160,Warning, % "Move Items operation has been disabled."
		}
			
		If WinExist("Move Items") && MoveTo=1
		{
			WinKill
			MsgBox,262160,Warning, % "Move Items operation has been disabled."
		}
			
		If WinActive("Copy Items") && CopyTo=1
		{
			WinKill
			MsgBox,262160,Warning, % "Copy Items operation has been disabled."
		}
			
		If WinExist("Copy Items") && CopyTo=1
		{
			WinKill
			MsgBox,262160,Warning, % "Copy Items operation has been disabled."
		}
			
		If WinActive("Copying...") && (CopyTo=1 || Copy=1)
		{
			WinKill
			MsgBox,262160,Warning, % "Copy operation has been disabled."
		}
		
		If WinExist("Copying...") && (CopyTo=1 || Copy=1)
		{
			WinKill
			MsgBox,262160,Warning, % "Copy operation has been disabled."
		}
		
		If WinActive("Copy") && (CopyTo=1 || Copy=1)
		{
			WinKill
			MsgBox,262160,Warning, % "Copy operation has been disabled."
		}
			
		If WinExist("Copy") && (CopyTo=1 || Copy=1)
		{
			WinKill
			MsgBox,262160,Warning, % "Copy operation has been disabled."
		}
		

		sleep 200

	}
	return
	
	ActiveControlIsOfClass(Class) {
		ControlGetFocus, FocusedControl, A
		ControlGet, FocusedControlHwnd, Hwnd,, %FocusedControl%, A
		WinGetClass, FocusedControlClass, ahk_id %FocusedControlHwnd%
		return (FocusedControlClass=Class)
	}
	MouseIsOverOpenWindow(WinTitle) {
		MouseGetPos, , , id, control
		if (control="DirectUIHWND2" or control="DirectUIHWND1" or control="ToolbarWindow321" ){
			return WinExist(WinTitle . " ahk_id " . id)
		}
	}

	

	
	

	
GetMacAddress(delimiter := "-", case := True)
	{
		if (DllCall("iphlpapi.dll\GetAdaptersInfo", "ptr", 0, "uint*", size) = 111) && !(VarSetCapacity(buf, size, 0))
			throw Exception("Memory allocation failed for IP_ADAPTER_INFO struct", -1)
		if (DllCall("iphlpapi.dll\GetAdaptersInfo", "ptr", &buf, "uint*", size) != 0)
			throw Exception("GetAdaptersInfo failed with error: " A_LastError, -1)
		addr := &buf, MAC_ADDRESS := []
		while (addr) {
			loop % NumGet(addr+0, 396 + A_PtrSize, "uint")
				mac .= Format("{:02" (case ? "X" : "x") "}", NumGet(addr+0, 400 + A_PtrSize + A_Index - 1, "uchar")) "" delimiter ""
			MAC_ADDRESS[A_Index] := SubStr(mac, 1, -1), mac := ""
			addr := NumGet(addr+0, "uptr")
		}
		return MAC_ADDRESS
	}
	