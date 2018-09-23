#r "paket: nuget FSharp.Date //"
#load ".fake/DayCountConventions.fsx/intellisense.fsx"


type DaycountConvention = 
    | DC30E360
    | DC30360US
    | DCACT360
    | DCACT365
    | DCACTACTISDA
    | DCBUS252

[<Measure>] type days

open System

module DayCount = 
    open System
    type DaysBetween = DaycountConvention -> DateTime -> DateTime -> int<days>

    let actualDaysBetween (startDate:DateTime) (endDate:DateTime) = int ((endDate.Date.Subtract(startDate.Date).TotalDays)) *  1<days>
    let rec ``30EdaysBetween`` (startDate:DateTime) (endDate:DateTime) =
        if (startDate.Day = 31) then 
            ``30EdaysBetween`` (DateTime(startDate.Year, startDate.Month, 30)) endDate
        else if (endDate.Day = 31) then
            ``30EdaysBetween`` startDate (DateTime(endDate.Year, endDate.Month, 30))
        else
            actualDaysBetween startDate endDate
    
    let daysBetween convention (startDate:DateTime) (endDate:DateTime) =
        match convention with
        | DCACT360
        | DCACTACTISDA
        | DCACT365 -> (endDate - startDate).TotalDays


