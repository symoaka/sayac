#Requires AutoHotkey v2.0
#SingleInstance Force
Persistent

SetWorkingDir A_ScriptDir

global dataFile := A_ScriptDir "\sayac_data.ini"
global counters := []
global miniVisible := true
global miniLocked := true
global miniDisplayMode := "3"
global miniGui := 0
global miniRebuilding := false
global mainGui := 0
global cardControls := []
global currentHotkey := "#SC028"
global mainHotkey := "^!s"
global listeningHK := false
global settingsOpen := false
global settingsGui := 0
global appSettingsOpen := false
global hkTimerFn := 0
global palette := [
    "0f0f0f", "111111", "1A1A1A", "2a2a2a", "3A3A3A", "FFFFFF",
    "E8E8E8", "AAAAAA", "F87171", "FB923C", "FACC15", "A3E635",
    "4ADE80", "2DD4BF", "38BDF8", "60A5FA", "818CF8", "A78BFA",
    "C084FC", "F472B6", "F43F5E", "14B8A6", "22C55E", "64748B"
]
global hkKeys := BuildHotkeyKeys()

LoadData()
MakeTray()
MakeMainGui()
RebuildMini()
ApplyHotkeys()
SetTimer UpdateMini, 1000
SetTimer UpdateCards, 1000
OnMessage(0x0201, MiniMouseDown)
OnMessage(0x0202, MiniMouseUp)
OnExit SaveOnExit
if !miniLocked
    SetTimer SaveMiniPosition, 1000

SaveOnExit(*) {
    SaveData()
}

MakeTray() {
    A_TrayMenu.Delete()
    A_TrayMenu.Add("Sayaclari Ac/Kapat", ToggleMain)
    A_TrayMenu.Add("Miniyi Ac/Kapat", ToggleMini)
    A_TrayMenu.Add("Mini Kilidi Ac/Kapat", ToggleMiniLock)
    A_TrayMenu.Add("Ayarlar", OpenSettingsPage)
    A_TrayMenu.Add()
    A_TrayMenu.Add("Cikis", (*) => ExitApp())
    A_TrayMenu.Default := "Sayaclari Ac/Kapat"
    TraySetIcon "shell32.dll", 266
}

LoadData() {
    global dataFile, counters, currentHotkey, mainHotkey, miniLocked, miniDisplayMode

    counters := []
    if !FileExist(dataFile)
        return

    currentHotkey := IniRead(dataFile, "global", "hotkey", currentHotkey)
    mainHotkey := IniRead(dataFile, "global", "mainHotkey", mainHotkey)
    miniLocked := (IniRead(dataFile, "global", "miniLocked", miniLocked ? 1 : 0) + 0) ? true : false
    miniDisplayMode := NormalizeMiniDisplayMode(IniRead(dataFile, "global", "miniDisplayMode", miniDisplayMode))

    idx := 1
    Loop {
        section := "counter" idx
        name := IniRead(dataFile, section, "name", "")
        targetRaw := IniRead(dataFile, section, "target", "")
        if (name = "" || targetRaw = "")
            break

        bgColor := CleanColor(IniRead(dataFile, section, "bgColor", "0f0f0f"), "0f0f0f")
        textColor := CleanColor(IniRead(dataFile, section, "textColor", "FFFFFF"), "FFFFFF")
        bgTransparent := IniRead(dataFile, section, "bgTransparent", 0) + 0
        autoBoxSize := IniRead(dataFile, section, "autoBoxSize", 1) + 0
        pinned := IniRead(dataFile, section, "pinned", 0) + 0
        fontSize := Clamp(IniRead(dataFile, section, "fontSize", 15) + 0, 8, 60)
        bgAlpha := Clamp(IniRead(dataFile, section, "bgAlpha", 220) + 0, 0, 255)
        posX := Clamp(IniRead(dataFile, section, "posX", DefaultPosX()) + 0, 0, A_ScreenWidth)
        posY := Clamp(IniRead(dataFile, section, "posY", DefaultPosY()) + 0, 0, A_ScreenHeight)
        boxWidth := Clamp(IniRead(dataFile, section, "boxWidth", DefaultBoxWidth()) + 0, 180, Max(180, A_ScreenWidth))
        rowHeight := Max(Clamp(IniRead(dataFile, section, "rowHeight", DefaultRowHeight(fontSize)) + 0, 18, 120), fontSize + 8)

        counters.Push(MakeCounter(name, targetRaw + 0, bgColor, textColor, fontSize, bgAlpha, posX, posY, boxWidth, rowHeight, bgTransparent, autoBoxSize, pinned))
        idx += 1
    }
}

SaveData() {
    global dataFile, counters, currentHotkey, mainHotkey, miniLocked, miniDisplayMode

    if FileExist(dataFile)
        FileDelete dataFile

    IniWrite currentHotkey, dataFile, "global", "hotkey"
    IniWrite mainHotkey, dataFile, "global", "mainHotkey"
    IniWrite (miniLocked ? 1 : 0), dataFile, "global", "miniLocked"
    IniWrite miniDisplayMode, dataFile, "global", "miniDisplayMode"
    for idx, c in counters {
        section := "counter" idx
        IniWrite c.name, dataFile, section, "name"
        IniWrite c.target, dataFile, section, "target"
        IniWrite c.bgColor, dataFile, section, "bgColor"
        IniWrite c.textColor, dataFile, section, "textColor"
        IniWrite c.bgTransparent, dataFile, section, "bgTransparent"
        IniWrite c.autoBoxSize, dataFile, section, "autoBoxSize"
        IniWrite c.pinned, dataFile, section, "pinned"
        IniWrite c.fontSize, dataFile, section, "fontSize"
        IniWrite c.bgAlpha, dataFile, section, "bgAlpha"
        IniWrite c.posX, dataFile, section, "posX"
        IniWrite c.posY, dataFile, section, "posY"
        IniWrite c.boxWidth, dataFile, section, "boxWidth"
        IniWrite c.rowHeight, dataFile, section, "rowHeight"
    }
}

MakeCounter(name, target, bgColor := "0f0f0f", textColor := "FFFFFF", fontSize := 15, bgAlpha := 220, posX := "", posY := "", boxWidth := "", rowHeight := "", bgTransparent := false, autoBoxSize := true, pinned := false) {
    if (posX = "")
        posX := DefaultPosX()
    if (posY = "")
        posY := DefaultPosY()
    if (boxWidth = "")
        boxWidth := DefaultBoxWidth()
    if (rowHeight = "")
        rowHeight := DefaultRowHeight(fontSize)

    return {
        name: name,
        target: target + 0,
        bgColor: CleanColor(bgColor, "0f0f0f"),
        textColor: CleanColor(textColor, "FFFFFF"),
        bgTransparent: bgTransparent ? true : false,
        autoBoxSize: autoBoxSize ? true : false,
        pinned: pinned ? true : false,
        fontSize: Clamp(fontSize + 0, 8, 60),
        bgAlpha: Clamp(bgAlpha + 0, 0, 255),
        posX: Clamp(posX + 0, 0, A_ScreenWidth),
        posY: Clamp(posY + 0, 0, A_ScreenHeight),
        boxWidth: Clamp(boxWidth + 0, 180, Max(180, A_ScreenWidth)),
        rowHeight: Max(Clamp(rowHeight + 0, 18, 120), fontSize + 8)
    }
}

CleanColor(value, fallback) {
    value := StrReplace(Trim(value), "#", "")
    if RegExMatch(value, "i)^[0-9a-f]{6}$")
        return value
    return fallback
}

Clamp(value, minValue, maxValue) {
    value := Round(value)
    if (value < minValue)
        return minValue
    if (value > maxValue)
        return maxValue
    return value
}

DefaultPosX() {
    return Max(0, A_ScreenWidth - DefaultBoxWidth() - 20)
}

DefaultPosY() {
    return Max(0, A_ScreenHeight - 130)
}

DefaultBoxWidth() {
    return Clamp(360, 180, Max(180, A_ScreenWidth))
}

DefaultRowHeight(fontSize := 15) {
    return Max(24, (fontSize + 0) + 10)
}

TransparentKeyColor() {
    return "010203"
}

NormalizeMiniDisplayMode(value) {
    value := StrLower(Trim(value))
    if (value = "1" || value = "3" || value = "5" || value = "all")
        return value
    return "3"
}

MiniDisplayLimit() {
    global counters, miniDisplayMode

    mode := NormalizeMiniDisplayMode(miniDisplayMode)
    if (mode = "all")
        return counters.Length
    return Min(mode + 0, counters.Length)
}

MiniVisibleIndices() {
    global counters

    if (counters.Length = 0)
        return [0]

    result := []
    limit := MiniDisplayLimit()
    for idx, c in counters {
        if c.pinned
            result.Push(idx)
        if (result.Length >= limit)
            return result
    }

    for idx, c in counters {
        if !c.pinned
            result.Push(idx)
        if (result.Length >= limit)
            break
    }

    return result
}

NowUnixUtc() {
    return DateDiff(A_NowUTC, "19700101000000", "Seconds")
}

FormatDiff(diff) {
    if (diff <= 0)
        return "Tamamlandi"

    total := Floor(diff)
    days := total // 86400
    rest := Mod(total, 86400)
    hours := rest // 3600
    rest := Mod(rest, 3600)
    mins := rest // 60
    secs := Mod(rest, 60)
    clock := Format("{:02}:{:02}:{:02}", hours, mins, secs)

    if (days > 0)
        return days " gun " clock
    return clock
}

FormatTarget(target) {
    try {
        localStamp := DateAdd("19700101000000", (target + 0) - (3 * 3600), "Seconds")
        return FormatTime(localStamp, "yyyy-MM-dd HH:mm")
    } catch {
        return "-"
    }
}

TargetToPickerStamp(target) {
    try {
        return DateAdd("19700101000000", (target + 0) - (3 * 3600), "Seconds")
    } catch {
        return A_Now
    }
}

PickerControlsToTarget(dateCtrl, timeCtrl) {
    try {
        dateValue := dateCtrl.Value
        timeValue := timeCtrl.Value
        if (dateValue = "" || timeValue = "")
            return ""
        stamp := SubStr(dateValue, 1, 8) SubStr(timeValue, 9, 4) "00"
        return DateDiff(stamp, "19700101000000", "Seconds") + (3 * 3600)
    } catch {
        return ""
    }
}

RebuildMini() {
    global counters, miniGui, miniVisible, miniRebuilding, miniLocked

    if miniRebuilding
        return
    miniRebuilding := true

    if IsObject(miniGui) {
        try miniGui.Destroy()
    }

    style := counters.Length ? counters[1] : MakeCounter("", 0)
    layout := CalcMiniLayout()
    visibleCount := layout.visibleCount
    winWidth := layout.winWidth
    winHeight := layout.winHeight
    textWidth := layout.textWidth
    hasTransparentRows := HasVisibleTransparentRows(visibleCount)
    backColor := (hasTransparentRows && miniLocked) ? TransparentKeyColor() : style.bgColor

    miniOptions := "+AlwaysOnTop -Caption +ToolWindow"
    if miniLocked
        miniOptions .= " +E0x20"
    miniGui := Gui(miniOptions, "Sayac Mini")
    miniGui.BackColor := backColor
    miniGui.MarginX := 10
    miniGui.MarginY := 6
    miniGui.SetFont("s" style.fontSize " c" style.textColor, "Segoe UI")

    Loop visibleCount {
        rowStyle := GetMiniRowStyle(layout.indices[A_Index])
        y := layout.rowY[A_Index]
        rowHeight := layout.rowHeights[A_Index]

        if !rowStyle.bgTransparent
            miniGui.AddText("x0 y" y " w" winWidth " h" rowHeight " Background" rowStyle.bgColor)

        if rowStyle.bgTransparent {
            miniGui.SetFont("s" rowStyle.fontSize " c000000", "Segoe UI")
            miniGui.AddText("x12 y" (y + 2) " w" textWidth " h" rowHeight " vMS" A_Index " BackgroundTrans", "")
        }

        miniGui.SetFont("s" rowStyle.fontSize " c" rowStyle.textColor, "Segoe UI")
        miniGui.AddText("x10 y" y " w" textWidth " h" rowHeight " vM" A_Index " BackgroundTrans", "")
    }

    miniGui.Show("x" style.posX " y" style.posY " w" winWidth " h" winHeight " NoActivate")
    if (hasTransparentRows && miniLocked)
        WinSetTransColor TransparentKeyColor(), "ahk_id " miniGui.Hwnd
    else
        WinSetTransparent style.bgAlpha, "ahk_id " miniGui.Hwnd
    UpdateMini()

    if !miniVisible
        miniGui.Hide()

    miniRebuilding := false
}

CalcMiniLayout() {
    global counters

    indices := MiniVisibleIndices()
    visibleCount := indices.Length
    rowHeights := []
    rowY := []
    totalHeight := 12

    winWidth := 180
    now := NowUnixUtc()
    Loop visibleCount {
        counterIdx := indices[A_Index]
        rowStyle := GetMiniRowStyle(counterIdx)
        rowHeight := rowStyle.autoBoxSize ? DefaultRowHeight(rowStyle.fontSize) : Max(rowStyle.rowHeight, rowStyle.fontSize + 8)
        rowHeights.Push(rowHeight)
        rowY.Push(totalHeight - 6)
        totalHeight += rowHeight

        if rowStyle.autoBoxSize {
            line := GetMiniLine(counterIdx, now)
            rowWidth := Clamp(Round((StrLen(line) * rowStyle.fontSize * 0.64) + 30), 180, Max(180, A_ScreenWidth))
        } else {
            rowWidth := rowStyle.boxWidth
        }
        winWidth := Max(winWidth, rowWidth)
    }

    return {
        indices: indices,
        visibleCount: visibleCount,
        rowHeights: rowHeights,
        rowY: rowY,
        winWidth: winWidth,
        winHeight: totalHeight,
        textWidth: Max(40, winWidth - 20)
    }
}

GetMiniRowStyle(idx) {
    global counters

    if (idx >= 1 && idx <= counters.Length)
        return counters[idx]
    if counters.Length
        return counters[1]
    return MakeCounter("", 0)
}

HasVisibleTransparentRows(visibleCount) {
    indices := MiniVisibleIndices()
    Loop visibleCount {
        if GetMiniRowStyle(indices[A_Index]).bgTransparent
            return true
    }
    return false
}

HasVisibleAutoRows(visibleCount) {
    indices := MiniVisibleIndices()
    Loop visibleCount {
        if GetMiniRowStyle(indices[A_Index]).autoBoxSize
            return true
    }
    return false
}

GetMiniLine(idx, now) {
    global counters

    if (idx >= 1 && idx <= counters.Length) {
        c := counters[idx]
        return c.name ": " FormatDiff(c.target - now)
    }
    return ""
}

ApplyMini(c) {
    RebuildMini()
}

UpdateMini() {
    global counters, miniGui

    if !IsObject(miniGui)
        return

    now := NowUnixUtc()
    indices := MiniVisibleIndices()
    Loop indices.Length {
        idx := A_Index
        line := GetMiniLine(indices[A_Index], now)
        try miniGui["MS" idx].Text := line
        try miniGui["M" idx].Text := line
    }

    MaybeResizeMiniAuto()
}

MaybeResizeMiniAuto() {
    global counters, miniGui, miniRebuilding

    visibleCount := MiniVisibleIndices().Length
    if !(counters.Length && IsObject(miniGui) && HasVisibleAutoRows(visibleCount)) || miniRebuilding
        return

    layout := CalcMiniLayout()
    try miniGui.GetPos(&currentX, &currentY, &currentW, &currentH)
    catch
        return

    if (currentW != layout.winWidth || currentH != layout.winHeight)
        RebuildMini()
}

MiniMouseDown(wParam, lParam, msg, hwnd) {
    global miniGui, miniLocked

    if miniLocked || !IsObject(miniGui) || !IsMiniWindowHwnd(hwnd)
        return

    PostMessage 0xA1, 2, 0,, "ahk_id " miniGui.Hwnd
}

MiniMouseUp(wParam, lParam, msg, hwnd) {
    global miniGui, miniLocked

    if !miniLocked && IsObject(miniGui)
        SaveMiniPosition()
}

IsMiniWindowHwnd(hwnd) {
    global miniGui

    if !IsObject(miniGui)
        return false
    root := DllCall("GetAncestor", "Ptr", hwnd, "UInt", 2, "Ptr")
    return root = miniGui.Hwnd || hwnd = miniGui.Hwnd
}

SaveMiniPosition(*) {
    global counters, miniGui

    if !IsObject(miniGui) || counters.Length = 0
        return

    try miniGui.GetPos(&x, &y, &w, &h)
    catch
        return

    changed := false
    for _, c in counters {
        if (c.posX != x || c.posY != y)
            changed := true
        c.posX := x
        c.posY := y
    }

    if changed
        SaveData()
}

MakeMainGui() {
    global mainGui

    mainGui := Gui("+AlwaysOnTop", "Sayaclar")
    mainGui.BackColor := "111111"
    mainGui.MarginX := 0
    mainGui.MarginY := 0
    mainGui.SetFont("s10 cE8E8E8", "Segoe UI")
    mainGui.OnEvent("Close", MainClose)
    mainGui.OnEvent("Escape", MainClose)

    mainGui.SetFont("s16 bold cFFFFFF", "Segoe UI")
    mainGui.AddText("x20 y16 w260 h28 BackgroundTrans", "Sayaclar")

    mainGui.SetFont("s9 cAAAAAA norm", "Segoe UI")
    mainGui.AddText("x20 y58 w180 h20 BackgroundTrans", "Sayac adi")
    mainGui.AddText("x252 y58 w100 h20 BackgroundTrans", "Hedef tarih")
    mainGui.AddText("x358 y58 w62 h20 BackgroundTrans", "Saat")

    mainGui.SetFont("s10 cFFFFFF", "Segoe UI")
    mainGui.AddEdit("x20 y80 w210 h28 vCName Background1A1A1A cFFFFFF")
    mainGui.AddDateTime("x252 y80 w100 h28 vCDate Choose" A_Now, "yyyy-MM-dd")
    mainGui.AddDateTime("x358 y80 w62 h28 vCTime 1 Choose" A_Now, "HH:mm")

    addBtn := mainGui.AddButton("x424 y80 w54 h28", "Ekle")
    addBtn.OnEvent("Click", AddCounter)

    settingsBtn := mainGui.AddButton("x486 y80 w48 h28", "Ayar")
    settingsBtn.OnEvent("Click", OpenSettingsPage)

    mainGui.AddText("x20 y126 w514 h1 Background2a2a2a")

    RenderCards()
    mainGui.Show("w554 h" MainWindowHeight() " Hide")
}

MainWindowHeight() {
    global counters
    contentHeight := counters.Length ? 148 + (counters.Length * 92) + 20 : 230
    return Min(Max(500, contentHeight), Max(500, A_ScreenHeight - 80))
}

MainClose(guiObj, *) {
    guiObj.Hide()
    return true
}

RenderCards() {
    global mainGui, counters, cardControls

    for ctrl in cardControls {
        try ctrl.Destroy()
    }
    cardControls := []

    y := 148
    if (counters.Length = 0) {
        mainGui.SetFont("s10 cAAAAAA", "Segoe UI")
        cardControls.Push(mainGui.AddText("x20 y" y " w514 h40 Center BackgroundTrans", "Henuz sayac yok. Ilk sayaci yukaridan ekleyin."))
        return
    }

    for idx, c in counters {
        cardControls.Push(mainGui.AddText("x20 y" y " w514 h78 Background1A1A1A"))

        mainGui.SetFont("s11 bold cFFFFFF", "Segoe UI")
        cardControls.Push(mainGui.AddText("x36 y" (y + 12) " w230 h22 BackgroundTrans", c.name))

        mainGui.SetFont("s9 cAAAAAA norm", "Segoe UI")
        cardControls.Push(mainGui.AddText("x36 y" (y + 42) " w170 h20 BackgroundTrans", "Hedef: " FormatTarget(c.target)))

        mainGui.SetFont("s14 bold cFFFFFF", "Segoe UI")
        cardControls.Push(mainGui.AddText("x220 y" (y + 28) " w180 h26 vK" idx " Right BackgroundTrans", FormatDiff(c.target - NowUnixUtc())))

        settingsBtn := mainGui.AddButton("x414 y" (y + 22) " w48 h28", "Ayar")
        settingsBtn.OnEvent("Click", OpenCounterSettings.Bind(idx))
        cardControls.Push(settingsBtn)

        deleteBtn := mainGui.AddButton("x470 y" (y + 22) " w48 h28", "Sil")
        deleteBtn.OnEvent("Click", DeleteCounter.Bind(idx))
        cardControls.Push(deleteBtn)

        y += 92
    }
}

UpdateCards() {
    global mainGui, counters

    if !IsObject(mainGui)
        return

    now := NowUnixUtc()
    for idx, c in counters {
        try mainGui["K" idx].Text := FormatDiff(c.target - now)
    }
}

AddCounter(*) {
    global mainGui, counters

    name := Trim(mainGui["CName"].Value)

    if (name = "") {
        MsgBox "Sayac adi bos olamaz.", "Uyari", 48
        return
    }

    target := ParsePickerTarget()
    if (target = "") {
        MsgBox "Gecerli bir tarih ve saat secin.", "Uyari", 48
        return
    }

    counters.Push(MakeCounter(name, target))
    SaveData()
    RenderCards()
    ResizeMainIfVisible()
    RebuildMini()

    mainGui["CName"].Value := ""
    mainGui["CDate"].Value := A_Now
    mainGui["CTime"].Value := A_Now
}

ParsePickerTarget() {
    global mainGui

    return PickerControlsToTarget(mainGui["CDate"], mainGui["CTime"])
}

DeleteCounter(idx, *) {
    global counters, settingsOpen, settingsGui

    if (idx < 1 || idx > counters.Length)
        return

    if settingsOpen {
        try settingsGui.Destroy()
        settingsOpen := false
        settingsGui := 0
    }

    counters.RemoveAt(idx)
    SaveData()
    RenderCards()
    ResizeMainIfVisible()
    RebuildMini()
}

ResizeMainIfVisible() {
    global mainGui

    if IsObject(mainGui) && WinExist("ahk_id " mainGui.Hwnd)
        mainGui.Show("w554 h" MainWindowHeight())
}

OpenCounterSettings(idx, *) {
    global counters, settingsOpen, settingsGui, palette

    if settingsOpen {
        try settingsGui.Destroy()
        settingsOpen := false
        settingsGui := 0
    }

    if (idx < 1 || idx > counters.Length)
        return

    settingsOpen := true
    c := counters[idx]

    sg := Gui("+AlwaysOnTop +ToolWindow", "Sayac Ayarlari")
    settingsGui := sg
    sg.BackColor := "111111"
    sg.MarginX := 0
    sg.MarginY := 0
    sg.SetFont("s10 cE8E8E8", "Segoe UI")
    sg.OnEvent("Close", SettingsClose)
    sg.OnEvent("Escape", SettingsClose)

    sg.SetFont("s13 bold cFFFFFF", "Segoe UI")
    sg.AddText("x18 y16 w330 h26 BackgroundTrans", "Sayac ayarlari")

    sg.SetFont("s9 cAAAAAA norm", "Segoe UI")
    sg.AddText("x18 y48 w160 h20 BackgroundTrans", "Sayac adi")
    nameEdit := sg.AddEdit("x18 y70 w342 h28 Background1A1A1A cFFFFFF", c.name)
    nameEdit.OnEvent("Change", OnCounterNameChange.Bind(idx))

    pickerStamp := TargetToPickerStamp(c.target)
    sg.AddText("x18 y108 w110 h20 BackgroundTrans", "Hedef tarih")
    sg.AddText("x142 y108 w70 h20 BackgroundTrans", "Saat")
    dateCtrl := sg.AddDateTime("x18 y130 w110 h28 Choose" pickerStamp, "yyyy-MM-dd")
    timeCtrl := sg.AddDateTime("x142 y130 w70 h28 1 Choose" pickerStamp, "HH:mm")
    dateCtrl.OnEvent("Change", OnCounterTargetChange.Bind(idx, dateCtrl, timeCtrl))
    timeCtrl.OnEvent("Change", OnCounterTargetChange.Bind(idx, dateCtrl, timeCtrl))

    pinCheck := sg.AddCheckBox("x230 y132 w130 h24 cE8E8E8", "Miniye sabitle")
    pinCheck.Value := c.pinned ? 1 : 0
    pinCheck.OnEvent("Click", OnCounterPinnedChange.Bind(idx))

    sg.AddText("x18 y180 w160 h20 BackgroundTrans", "Arka plan rengi")
    transparentBtn := sg.AddButton("x286 y178 w74 h24", "Transparan")

    sg.AddText("x18 y258 w160 h20 BackgroundTrans", "Yazi rengi")
    BuildPaletteRow(sg, idx, "text", 18, 282)

    sg.AddText("x18 y336 w160 h20 BackgroundTrans", "Font boyutu")
    fontValue := sg.AddText("x316 y336 w44 h20 Right BackgroundTrans", c.fontSize)
    fontSlider := sg.AddSlider("x18 y360 w342 h28 Range8-60 ToolTip", c.fontSize)

    autoBoxCheck := sg.AddCheckBox("x18 y400 w220 h24 cE8E8E8", "Otomatik kutu boyutu")
    autoBoxCheck.Value := c.autoBoxSize ? 1 : 0

    sg.AddText("x18 y436 w160 h20 BackgroundTrans", "Kutu genisligi")
    boxWidthValue := sg.AddText("x286 y436 w74 h20 Right BackgroundTrans", c.boxWidth)
    boxWidthSlider := sg.AddSlider("x18 y460 w342 h28 Range180-" Max(180, A_ScreenWidth) " ToolTip", c.boxWidth)
    boxWidthSlider.Enabled := !c.autoBoxSize
    boxWidthSlider.OnEvent("Change", OnBoxWidthChange.Bind(idx, boxWidthValue))

    sg.AddText("x18 y500 w160 h20 BackgroundTrans", "Kutu yuksekligi")
    rowHeightValue := sg.AddText("x286 y500 w74 h20 Right BackgroundTrans", c.rowHeight)
    rowHeightSlider := sg.AddSlider("x18 y524 w342 h28 Range18-120 ToolTip", c.rowHeight)
    rowHeightSlider.Enabled := !c.autoBoxSize
    rowHeightSlider.OnEvent("Change", OnRowHeightChange.Bind(idx, rowHeightValue))
    autoBoxCheck.OnEvent("Click", OnAutoBoxSizeChange.Bind(idx, boxWidthSlider, rowHeightSlider))
    fontSlider.OnEvent("Change", OnFontSizeChange.Bind(idx, fontValue, rowHeightValue, rowHeightSlider))

    sg.AddText("x18 y564 w160 h20 BackgroundTrans", "Arka plan saydamligi")
    alphaValue := sg.AddText("x316 y564 w44 h20 Right BackgroundTrans", c.bgAlpha)
    alphaSlider := sg.AddSlider("x18 y588 w342 h28 Range0-255 ToolTip", c.bgAlpha)
    alphaSlider.OnEvent("Change", OnAlphaChange.Bind(idx, alphaValue))
    BuildPaletteRow(sg, idx, "bg", 18, 204, alphaValue, alphaSlider)
    transparentBtn.OnEvent("Click", SetTransparentBg.Bind(idx, alphaValue, alphaSlider))

    sg.AddText("x18 y628 w160 h20 BackgroundTrans", "X konumu")
    xValue := sg.AddText("x286 y628 w74 h20 Right BackgroundTrans", c.posX)
    xSlider := sg.AddSlider("x18 y652 w342 h28 Range0-" A_ScreenWidth " ToolTip", c.posX)
    xSlider.OnEvent("Change", OnPosXChange.Bind(idx, xValue))

    sg.AddText("x18 y692 w160 h20 BackgroundTrans", "Y konumu")
    yValue := sg.AddText("x286 y692 w74 h20 Right BackgroundTrans", c.posY)
    ySlider := sg.AddSlider("x18 y716 w342 h28 Range0-" A_ScreenHeight " ToolTip", c.posY)
    ySlider.OnEvent("Change", OnPosYChange.Bind(idx, yValue))

    closeBtn := sg.AddButton("x260 y760 w100 h30", "Kapat")
    closeBtn.OnEvent("Click", SettingsClose.Bind(sg))

    sg.Show("w380 h808")
}

BuildPaletteRow(sg, idx, mode, startX, startY, alphaValueCtrl := 0, alphaSlider := 0) {
    global palette

    sw := 24
    gap := 6
    Loop palette.Length {
        color := palette[A_Index]
        col := Mod(A_Index - 1, 12)
        row := (A_Index - 1) // 12
        x := startX + (col * (sw + gap))
        y := startY + (row * (sw + gap))
        ctrl := sg.AddText("x" x " y" y " w" sw " h" sw " Background" color " Border 0x100")

        if (mode = "bg")
            ctrl.OnEvent("Click", SetBgColor.Bind(idx, color, alphaValueCtrl, alphaSlider))
        else
            ctrl.OnEvent("Click", SetTextColor.Bind(idx, color))
    }
}

SetBgColor(idx, color, alphaValueCtrl := 0, alphaSlider := 0, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return
    counters[idx].bgColor := color
    counters[idx].bgTransparent := false
    if (counters[idx].bgAlpha = 0) {
        counters[idx].bgAlpha := 220
        SyncAlphaControls(alphaValueCtrl, alphaSlider, counters[idx].bgAlpha)
    }
    ApplyMini(counters[idx])
}

SetTransparentBg(idx, alphaValueCtrl, alphaSlider, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    counters[idx].bgTransparent := true
    ApplyMini(counters[idx])
}

SyncAlphaControls(alphaValueCtrl, alphaSlider, value) {
    if IsObject(alphaValueCtrl)
        alphaValueCtrl.Text := value
    if IsObject(alphaSlider)
        alphaSlider.Value := value
}

OnCounterNameChange(idx, ctrl, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    name := Trim(ctrl.Value)
    if (name = "")
        return

    counters[idx].name := name
    SaveData()
    RenderCards()
    RebuildMini()
}

OnCounterTargetChange(idx, dateCtrl, timeCtrl, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    target := PickerControlsToTarget(dateCtrl, timeCtrl)
    if (target = "")
        return

    counters[idx].target := target
    SaveData()
    RenderCards()
    RebuildMini()
}

OnCounterPinnedChange(idx, ctrl, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    counters[idx].pinned := ctrl.Value = 1
    SaveData()
    RebuildMini()
}

SetTextColor(idx, color, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return
    counters[idx].textColor := color
    ApplyMini(counters[idx])
}

OnFontSizeChange(idx, valueCtrl, rowHeightValueCtrl, rowHeightSlider, slider, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    value := Clamp(slider.Value, 8, 60)
    counters[idx].fontSize := value
    counters[idx].rowHeight := Max(counters[idx].rowHeight, DefaultRowHeight(value))
    valueCtrl.Text := value
    rowHeightValueCtrl.Text := counters[idx].rowHeight
    rowHeightSlider.Value := counters[idx].rowHeight
    ApplyMini(counters[idx])
}

OnAutoBoxSizeChange(idx, boxWidthSlider, rowHeightSlider, checkBox, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    enabled := checkBox.Value = 1
    counters[idx].autoBoxSize := enabled
    boxWidthSlider.Enabled := !enabled
    rowHeightSlider.Enabled := !enabled
    ApplyMini(counters[idx])
}

OnBoxWidthChange(idx, valueCtrl, slider, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    value := Clamp(slider.Value, 180, Max(180, A_ScreenWidth))
    counters[idx].boxWidth := value
    valueCtrl.Text := value
    ApplyMini(counters[idx])
}

OnRowHeightChange(idx, valueCtrl, slider, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    value := Clamp(slider.Value, counters[idx].fontSize + 8, 120)
    counters[idx].rowHeight := value
    valueCtrl.Text := value
    ApplyMini(counters[idx])
}

OnAlphaChange(idx, valueCtrl, slider, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    value := Clamp(slider.Value, 0, 255)
    for _, c in counters
        c.bgAlpha := value
    valueCtrl.Text := value
    ApplyMini(counters[idx])
}

OnPosXChange(idx, valueCtrl, slider, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    value := Clamp(slider.Value, 0, A_ScreenWidth)
    for _, c in counters
        c.posX := value
    valueCtrl.Text := value
    ApplyMini(counters[idx])
}

OnPosYChange(idx, valueCtrl, slider, *) {
    global counters
    if (idx < 1 || idx > counters.Length)
        return

    value := Clamp(slider.Value, 0, A_ScreenHeight)
    for _, c in counters
        c.posY := value
    valueCtrl.Text := value
    ApplyMini(counters[idx])
}

SettingsClose(sg, *) {
    global settingsOpen, settingsGui

    SaveData()
    settingsOpen := false
    settingsGui := 0
    try sg.Destroy()
    return true
}

OpenHotkeyDlg(*) {
    OpenSettingsPage()
}

OpenSettingsPage(*) {
    global currentHotkey, mainHotkey, appSettingsOpen, miniLocked, miniDisplayMode

    if appSettingsOpen {
        MsgBox "Ayarlar penceresi zaten acik.", "Uyari", 48
        return
    }

    appSettingsOpen := true
    sg := Gui("+AlwaysOnTop +ToolWindow", "Ayarlar")
    sg.BackColor := "111111"
    sg.MarginX := 0
    sg.MarginY := 0
    sg.SetFont("s10 cE8E8E8", "Segoe UI")
    sg.OnEvent("Close", AppSettingsClose)
    sg.OnEvent("Escape", AppSettingsClose)

    sg.SetFont("s14 bold cFFFFFF", "Segoe UI")
    sg.AddText("x18 y16 w330 h28 BackgroundTrans", "Ayarlar")

    sg.SetFont("s10 cAAAAAA norm", "Segoe UI")
    sg.AddText("x18 y60 w330 h22 BackgroundTrans", "Ozel kisayollar")

    sg.SetFont("s10 cE8E8E8", "Segoe UI")
    sg.AddText("x18 y96 w150 h22 BackgroundTrans", "Miniyi ac/kapat")
    miniValue := sg.AddText("x178 y96 w110 h22 cFFFFFF BackgroundTrans", DisplayHotkey(currentHotkey))
    miniBtn := sg.AddButton("x300 y92 w72 h28", "Dinle")

    sg.AddText("x18 y136 w150 h22 BackgroundTrans", "Ana paneli ac/kapat")
    mainValue := sg.AddText("x178 y136 w110 h22 cFFFFFF BackgroundTrans", DisplayHotkey(mainHotkey))
    mainBtn := sg.AddButton("x300 y132 w72 h28", "Dinle")

    sg.SetFont("s10 cAAAAAA norm", "Segoe UI")
    sg.AddText("x18 y184 w330 h22 BackgroundTrans", "Mini kontroller")

    sg.SetFont("s10 cE8E8E8", "Segoe UI")
    lockCheck := sg.AddCheckBox("x18 y216 w170 h24 cE8E8E8", "Mini kilitli")
    lockCheck.Value := miniLocked ? 1 : 0
    lockCheck.OnEvent("Click", OnMiniLockedSettingChange)

    sg.AddText("x206 y216 w72 h22 BackgroundTrans", "Goster")
    modeList := sg.AddDropDownList("x278 y212 w94", ["1", "3", "5", "Hepsi"])
    modeList.Choose(MiniDisplayModeIndex())
    modeList.OnEvent("Change", OnMiniDisplayModeChange)

    status := sg.AddText("x18 y264 w354 h44 cAAAAAA BackgroundTrans", "Bir Dinle dugmesine basin, sonra modifier + tus kombinasyonunu girin.")

    miniBtn.OnEvent("Click", StartHKListen.Bind("mini", miniValue, status))
    mainBtn.OnEvent("Click", StartHKListen.Bind("main", mainValue, status))

    closeBtn := sg.AddButton("x272 y324 w100 h30", "Kapat")
    closeBtn.OnEvent("Click", AppSettingsClose.Bind(sg))

    sg.Show("w390 h374")
}

MiniDisplayModeIndex() {
    global miniDisplayMode

    mode := NormalizeMiniDisplayMode(miniDisplayMode)
    if (mode = "1")
        return 1
    if (mode = "3")
        return 2
    if (mode = "5")
        return 3
    return 4
}

OnMiniLockedSettingChange(ctrl, *) {
    global miniLocked

    wanted := ctrl.Value = 1
    if (miniLocked = wanted)
        return

    miniLocked := wanted
    if miniLocked {
        SaveMiniPosition()
        SetTimer SaveMiniPosition, 0
    } else {
        SetTimer SaveMiniPosition, 1000
    }
    SaveData()
    RebuildMini()
}

OnMiniDisplayModeChange(ctrl, *) {
    global miniDisplayMode

    selected := ctrl.Text
    miniDisplayMode := selected = "Hepsi" ? "all" : NormalizeMiniDisplayMode(selected)
    SaveData()
    RebuildMini()
}

StartHKListen(action, valueCtrl, statusCtrl, btn, *) {
    global listeningHK, hkTimerFn

    if listeningHK
        return

    listeningHK := true
    btn.Enabled := false
    statusCtrl.Text := "Kombinasyona basin..."
    hkTimerFn := DoListenKey.Bind(action, valueCtrl, statusCtrl, btn)
    SetTimer hkTimerFn, 50
}

DoListenKey(action, valueCtrl, statusCtrl, btn) {
    global listeningHK, hkTimerFn, currentHotkey, mainHotkey, hkKeys

    mods := ""
    modText := ""
    if GetKeyState("LWin", "P") || GetKeyState("RWin", "P") {
        mods .= "#"
        modText .= "Win+"
    }
    if GetKeyState("Ctrl", "P") {
        mods .= "^"
        modText .= "Ctrl+"
    }
    if GetKeyState("Alt", "P") {
        mods .= "!"
        modText .= "Alt+"
    }
    if GetKeyState("Shift", "P") {
        mods .= "+"
        modText .= "Shift+"
    }

    for item in hkKeys {
        if !GetKeyState(item.key, "P")
            continue

        if (mods = "") {
            statusCtrl.Text := "En az bir modifier kullanin: Win, Ctrl, Alt veya Shift."
            return
        }

        newHotkey := mods item.key
        if (action = "mini" && newHotkey = mainHotkey) || (action = "main" && newHotkey = currentHotkey) {
            statusCtrl.Text := "Bu kisayol baska bir islem icin kullaniliyor."
            return
        }

        SetTimer hkTimerFn, 0
        listeningHK := false
        btn.Enabled := true

        if (action = "mini") {
            oldHotkey := currentHotkey
            currentHotkey := newHotkey
            actionName := "Mini"
        } else {
            oldHotkey := mainHotkey
            mainHotkey := newHotkey
            actionName := "Ana panel"
        }

        try Hotkey oldHotkey, "Off"
        ApplyHotkeys()
        SaveData()

        valueCtrl.Text := DisplayHotkey(newHotkey)
        statusCtrl.Text := actionName " kisayolu: " modText item.label
        return
    }
}

AppSettingsClose(sg, *) {
    global listeningHK, hkTimerFn, appSettingsOpen

    if IsObject(hkTimerFn) {
        try SetTimer hkTimerFn, 0
    }
    hkTimerFn := 0
    listeningHK := false
    appSettingsOpen := false
    try sg.Destroy()
    return true
}

BuildHotkeyKeys() {
    keys := []

    for key in StrSplit("ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        keys.Push({key: key, label: key})

    Loop 10 {
        value := A_Index - 1
        keys.Push({key: "" value, label: "" value})
    }

    Loop 12 {
        value := "F" A_Index
        keys.Push({key: value, label: value})
    }

    keys.Push({key: "SC028", label: "'"})
    keys.Push({key: "Space", label: "Space"})
    keys.Push({key: "Enter", label: "Enter"})
    keys.Push({key: "Tab", label: "Tab"})
    keys.Push({key: "Escape", label: "Esc"})
    keys.Push({key: "Backspace", label: "Backspace"})
    keys.Push({key: "Delete", label: "Delete"})
    keys.Push({key: "Insert", label: "Insert"})
    keys.Push({key: "Home", label: "Home"})
    keys.Push({key: "End", label: "End"})
    keys.Push({key: "PgUp", label: "PageUp"})
    keys.Push({key: "PgDn", label: "PageDown"})
    keys.Push({key: "Up", label: "Up"})
    keys.Push({key: "Down", label: "Down"})
    keys.Push({key: "Left", label: "Left"})
    keys.Push({key: "Right", label: "Right"})

    return keys
}

DisplayHotkey(hk) {
    out := ""
    i := 1
    while (i <= StrLen(hk)) {
        ch := SubStr(hk, i, 1)
        if (ch = "#")
            out .= "Win+"
        else if (ch = "^")
            out .= "Ctrl+"
        else if (ch = "!")
            out .= "Alt+"
        else if (ch = "+")
            out .= "Shift+"
        else
            break
        i += 1
    }

    key := SubStr(hk, i)
    if (key = "SC028")
        key := "'"
    return out key
}

ToggleMain(*) {
    global mainGui

    if !IsObject(mainGui)
        return

    if WinExist("ahk_id " mainGui.Hwnd)
        mainGui.Hide()
    else {
        RenderCards()
        mainGui.Show("w554 h" MainWindowHeight())
    }
}

ToggleMini(*) {
    global miniGui, miniVisible

    if !IsObject(miniGui)
        RebuildMini()

    if miniVisible {
        miniGui.Hide()
        miniVisible := false
    } else {
        miniGui.Show("NoActivate")
        miniVisible := true
    }
}

ToggleMiniLock(*) {
    global miniLocked

    miniLocked := !miniLocked
    if miniLocked {
        SaveMiniPosition()
        SetTimer SaveMiniPosition, 0
    } else {
        SetTimer SaveMiniPosition, 1000
    }
    SaveData()
    RebuildMini()
}

ToggleMiniHK(*) {
    ToggleMini()
}

ApplyHotkey() {
    ApplyHotkeys()
}

ApplyHotkeys() {
    global currentHotkey, mainHotkey

    try Hotkey currentHotkey, ToggleMiniHK, "On"
    try Hotkey mainHotkey, ToggleMain, "On"
}
