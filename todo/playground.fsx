//#load @"c:\packages.fsx"

type Undefined = Exception
open System.Web.Security
open System.Runtime.Remoting.Metadata.W3cXsd2001
//...


module InterestRate = 
    type DaycountConvention = 
        | DC30E360
        | DC30360US
        | DCACT360
        | DCACT365
        | DCACTACTISDA
        | DCBUS252

    type CompoundFrequency = 
        | Annually
        | Monthly
        | Daily
        | Continous

    type Compound = 
        | Simple
        | Compounded of CompoundFrequency

    type InterestRate = {
        Rate : decimal
        Compound: Compound;
        Daycount : DaycountConvention
    }

    let create daycount compound rate =  {
        Rate = rate;
        Compound = compound;
        Daycount = daycount
    }


    let treasury = create DCACTACTISDA Simple

    let cdi = create DCBUS252 (Compounded Annually)


InterestRate.cdi 0.12M

InterestRate.treasury 0.2M

[<Measure>] type days
[<Measure>] type months
[<Measure>] type years


open System
let ``30EdaysBetween v1`` (startDate:DateTime) (endDate:DateTime) =
    let mutable d1 = startDate.Day 
    let mutable d2 = endDate.Day 
    let y1 = startDate.Year 
    let y2 = endDate.Year 
    let m1 = startDate.Month 
    let m2 = endDate.Month 

    if (d1 = 31) then d1 <- 30
    if (d2 = 31) then d2 <- 30



let (|Date|) (date:DateTime) = (date.Year, date.Month, date.Day)

//val 30xDaysBetween: years:int<years> -> months:int<months> -> days:int<days> -> int<days>
let ``30xDaysBetween`` y1 m1 d1 y2 m2 d2 : int<days> =
    let years  = (y2 - y1) * 1<years>
    let months = (m2 - m1) * 1<months>
    let days   = (d2 - d1) * 1<days>
    (years * 360<days/years>) + (months * 30<days/months>) + days

//ok
let rec ``30EdaysBetween`` (startDate:DateTime) (endDate:DateTime) = 
    match (startDate, endDate) with
    | ( Date (y1, m1, 31), _) ->  ``30EdaysBetween`` (DateTime(y1, m1, 30)) endDate
    | ( _, Date (y2, m2, 31)) -> ``30EdaysBetween`` startDate (DateTime(y2, m2, 30))
    | (Date(y1, m1, d1), Date(y2, m2, d2)) -> ``30xDaysBetween`` y1 m1 d1 y2 m2 d2


//------------------------ US
let (|EndOfMonth|_|) (date:DateTime) =
    if (date.AddDays(1.).Month <> date.Month) then Some date.Month
    else None

let rec ``30USdaysBetween`` (startDate:DateTime) (endDate:DateTime) =
    match (startDate, endDate) with
    | (EndOfMonth 2, EndOfMonth 2) -> (Date)
    | ( _, Date (y2, m2, 31)) -> ``30EdaysBetween`` startDate (DateTime(y2, m2, 30))
    | (Date(y1, m1, d1), Date(y2, m2, d2)) -> ``30xDaysBetween`` y1 m1 d1 y2 m2 d2



type Date360  = { year: int; month : int; day: int}
let teste startDate endDate  =
    match (startDate, endDate) with
    | (EndOfMonth 2, EndOfMonth 2) -> 

