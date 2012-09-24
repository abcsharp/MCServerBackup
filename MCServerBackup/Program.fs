open System
open System.Windows.Forms
open System.Drawing
open System.IO

type Form1(interval) as it =
    inherit Form()

    let backupInterval = interval
    let mutable currentTicks = 0
    let timer = new Timer(Interval = 1000)

    let label1 = new Label(Text = "設定時間:" + interval.ToString() + "秒", Location = Point(10, 15), Size = Size(160, 15))
    let label2 = new Label(Text = "次のバックアップまで:" + interval.ToString() + "秒", Location = Point(10, 35), Size = Size(160, 15))
    let deleteAllButton = new Button(Text = "全てのバックアップを削除", Location = Point(10, 55), Size = Size(175, 23))
    let deleteAllWithoutRecentButton = new Button(Text = "最新以外のバックアップを削除", Location = Point(10, 87), Size = Size(175, 23))

    let targetDir = Environment.CurrentDirectory + "\\backup"

    let rec CopyDirectory sourceDir destDir =
        let source = new DirectoryInfo(sourceDir)
        let dest = Directory.CreateDirectory(destDir)
        source.GetFiles()
        |> Array.iter (fun file -> File.Copy(file.FullName, dest.FullName + "\\" + file.Name))
        source.GetDirectories()
        |> Array.iter (fun dir -> CopyDirectory dir.FullName <| dest.FullName + "\\" + dir.Name)

    let CreateBackup () =
        let now = DateTime.Now
        let dirInfo =
            String.Format(targetDir + "\\world-{0:D4}-{1:D2}-{2:D2}-{3:D2}-{4:D2}", now.Year, now.Month, now.Day, now.Hour, now.Minute)
            |> Directory.CreateDirectory
        System.Threading.ThreadPool.QueueUserWorkItem(
            new System.Threading.WaitCallback(
                fun _ -> CopyDirectory (Environment.CurrentDirectory + "\\world") dirInfo.FullName))
        |> ignore

    do
        if Directory.Exists targetDir |> not then Directory.CreateDirectory targetDir |> ignore
        it.Text <- "MCBackup"
        it.FormBorderStyle <- FormBorderStyle.FixedSingle
        it.Size <- Size(200, 150)
        it.Controls.Add label1
        it.Controls.Add label2
        it.Controls.Add deleteAllButton
        it.Controls.Add deleteAllWithoutRecentButton
        currentTicks <- backupInterval
        timer.Tick
        |> Observable.subscribe
            (fun _ ->
                currentTicks <- currentTicks - 1
                label2.Text <- "次のバックアップまで:" + currentTicks.ToString() + "秒"
                if currentTicks = 0 then
                    CreateBackup()
                    currentTicks <- backupInterval)
        |> ignore
        deleteAllButton.Click
        |> Observable.subscribe
            (fun _ ->
                Directory.GetDirectories targetDir
                |> Array.iter (fun name -> Directory.Delete(name, true)))
        |> ignore
        deleteAllWithoutRecentButton.Click
        |> Observable.subscribe
            (fun _ ->
                let sortedDirs =
                    Directory.GetDirectories targetDir
                    |> Array.map (fun name -> new DirectoryInfo(name))
                    |> Array.sortBy (fun dir -> dir.CreationTime.Ticks)
                if sortedDirs.Length > 1 then
                    Array.sub sortedDirs 0 (sortedDirs.Length - 1)
                    |> Array.iter (fun dir -> dir.Delete(true)))
        |> ignore
        timer.Enabled <- true
           

[<EntryPoint>]
let main (args : string[]) =
    Application.EnableVisualStyles()
    Application.SetCompatibleTextRenderingDefault true
    let interval =
        match args.Length with
            | 0 -> 5400
            | _ -> int args.[0]
    if interval < 60 then
        1
    else
        Application.Run(new Form1(interval))
        0