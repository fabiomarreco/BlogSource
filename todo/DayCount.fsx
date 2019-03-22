open System

type DaycountConvention = 
    | DC30E360
    | DC30360US
    | DCACT360
    | DCACT365
    | DCACTACTISDA
    | DCBUS252

[<Measure>] type days
[<Measure>] type months
[<Measure>] type years




let actualDaysBetween 
    (startDate:DateTime) (endDate:DateTime) = 
    int ((endDate.Date.Subtract(startDate.Date).TotalDays)) *  1<days>

let ``30EdaysBetween`` (startDate:DateTime) (endDate:DateTime) =
    let mutable d1 = startDate.Day 
    let mutable d2 = endDate.Day 
    let y1 = startDate.Year 
    let y2 = endDate.Year 
    let m1 = startDate.Month 
    let m2 = endDate.Month 

    if (d1 = 31) then d1 <- 30
    if (d2 = 31) then d2 <- 30

    let years  = (y2 - y1) * 1<years>
    let months = (m2 - m1) * 1<months>
    let days   = (d2 - d1) * 1<days>

    (years * 360<days/years>) + (months * 30<days/months>) + days


let daysBetween convention  =
    match convention with
    | DCACT360
    | DCACTACTISDA
    | DCACT365 -> actualDaysBetween

