module DaycountConventions
open System

type DaycountConvention = 
    | DC30E360
    | DC30E360ISDA
    | DC30360US
    | DCACT360
    | DCACT365
    | DCACTACTISDA
    | DCBUS252

[<Measure>] type days
[<Measure>] type months
[<Measure>] type years

module Actual =
    let actualDaysBetween 
        (startDate:DateTime) (endDate:DateTime) = 
        int ((endDate.Date.Subtract(startDate.Date).TotalDays)) *  1<days>

module ``30360 v0`` = 
    type private DecodedDate = { Y: int; M: int; D : int } 

    let private decode (dt:DateTime) = { Y = dt.Year; M = dt.Month; D = dt.Day }

    let private daysBetween dd1 dd2 = 
        360<days/years> * (dd2.Y - dd1.Y) * 1<years>
          + 30<days/months> * (dd2.M - dd1.M) * 1<months>
          + (dd2.D - dd1.D) * 1<days>

    /// Date adjustment rules:
    ///  - If D1 is 31, then change D1 to 30.
    ///  - If D2 is 31, then change D2 to 30.
    let ``30EdaysBetween`` (startDate:DateTime) (endDate:DateTime) =
        let dd1 = decode startDate
        let dd2 = decode startDate

        let dd1 = if (dd1.D = 31) then { dd1 with D = 30 } else dd1
        let dd2 = if (dd2.D = 31) then { dd2 with D = 30 } else dd2
        daysBetween dd1 dd2

    let private (|EndOfMonth|_|) {Y = y; M = m; D = d} = 
        if (DateTime.DaysInMonth(y, m) = d) then Some m
        else None

    ///Date adjustment rules (more than one may take effect; apply them in order, and if a date is changed in one rule the changed value is used in the following rules):
    /// - If the investment is EOM and (Date1 is the last day of February) and (Date2 is the last day of February), then change D2 to 30.
    /// - If the investment is EOM and (Date1 is the last day of February), then change D1 to 30.
    /// - If D2 is 31 and D1 is 30 or 31, then change D2 to 30.
    /// - If D1 is 31, then change D1 to 30.
    let ``30USdaysBetween`` (startDate:DateTime) (endDate:DateTime) = 
        let rec adjust dd1 dd2 = 
            match (dd1, dd2) with 
            | EndOfMonth 2, EndOfMonth 2 -> adjust dd1 {dd2 with D = 30}
            | EndOfMonth 2, _ -> adjust {dd1 with D = 30} dd2
            | { Y=_; M=_; D = 30}, { Y=_; M=_; D = 31}
            | { Y=_; M=_; D = 31}, { Y=_; M=_; D = 31} -> adjust dd1 {dd2 with D = 30}
            | { Y=_; M=_; D = 31}, _ -> adjust {dd1 with D = 30} dd2
            | _ -> dd1, dd2

        let dd1, dd2 = adjust (decode startDate) (decode startDate)
        daysBetween dd1 dd2 

    ///Date adjustment rules:
    /// - If D1 is the last day of the month, then change D1 to 30.
    /// - If D2 is the last day of the month (unless Date2 is the maturity date and M2 is February), then change D2 to 30.
    let ``30EISDAdaysBetween`` (startDate:DateTime) (endDate:DateTime) =
        let dd1 = decode startDate
        let dd2 = decode startDate

        let dd1 = match dd1 with | EndOfMonth _ -> {dd1 with D = 30} | _ -> dd1
        let dd2 = match dd2 with | EndOfMonth x when x <> 2 -> {dd2 with D = 30} | _ -> dd2
        daysBetween dd1 dd2


//******************************************************************

module DC30360 = 
    type private DecodedDate = { Y: int; M: int; D : int } 

    let private decode (dt:DateTime) = { Y = dt.Year; M = dt.Month; D = dt.Day }

    let private daysBetween dd1 dd2 = 
        360<days/years> * (dd2.Y - dd1.Y) * 1<years>
          + 30<days/months> * (dd2.M - dd1.M) * 1<months>
          + (dd2.D - dd1.D) * 1<days>

    let private daysBetweenDatesWith adjustment startDate endDate = 
        let dd1 = decode startDate
        let dd2 = decode endDate 
        let (dd1, dd2) = adjustment dd1 dd2
        daysBetween dd1 dd2


    /// Date adjustment rules:
    ///  - If D1 is 31, then change D1 to 30.
    ///  - If D2 is 31, then change D2 to 30.
    let daysBetween30E =
        let adjustment dd1 dd2 = 
            let dd1 = if (dd1.D = 31) then { dd1 with D = 30 } else dd1
            let dd2 = if (dd2.D = 31) then { dd2 with D = 30 } else dd2
            dd1, dd2
        in daysBetweenDatesWith adjustment

    let private (|EndOfMonth|_|) {Y = y; M = m; D = d} = 
        if (DateTime.DaysInMonth(y, m) = d) then Some m
        else None

    ///Date adjustment rules (more than one may take effect; apply them in order, and if a date is changed in one rule the changed value is used in the following rules):
    /// - If the investment is EOM and (Date1 is the last day of February) and (Date2 is the last day of February), then change D2 to 30.
    /// - If the investment is EOM and (Date1 is the last day of February), then change D1 to 30.
    /// - If D2 is 31 and D1 is 30 or 31, then change D2 to 30.
    /// - If D1 is 31, then change D1 to 30.
    let daysBetween30US  = 
        let rec adjustment dd1 dd2 = 
            match (dd1, dd2) with 
            | EndOfMonth 2, EndOfMonth 2 -> adjustment dd1 {dd2 with D = 30}
            | EndOfMonth 2, _ -> adjustment {dd1 with D = 30} dd2
            | { Y=_; M=_; D = 30}, { Y=_; M=_; D = 31}
            | { Y=_; M=_; D = 31}, { Y=_; M=_; D = 31} -> adjustment dd1 {dd2 with D = 30}
            | { Y=_; M=_; D = 31}, _ -> adjustment {dd1 with D = 30} dd2
            | _ -> dd1, dd2
        in daysBetweenDatesWith adjustment

    ///Date adjustment rules:
    /// - If D1 is the last day of the month, then change D1 to 30.
    /// - If D2 is the last day of the month (unless Date2 is the maturity date and M2 is February), then change D2 to 30.
    let daysBetween30EISDA  =
        let adjustment dd1 dd2 = 
            let dd1 = match dd1 with | EndOfMonth _ -> {dd1 with D = 30} | _ -> dd1
            let dd2 = match dd2 with | EndOfMonth x when x <> 2 -> {dd2 with D = 30} | _ -> dd2
            dd1, dd2
        in daysBetweenDatesWith adjustment


module BusinessDays = 
    let daysBetween (startDate:DateTime) (endDate:DateTime) = 
        


let daysBetween convention  =
    match convention with
    | DCACT360
    | DCACTACTISDA
    | DCACT365 -> Actual.actualDaysBetween
    | DC30360US -> DC30360.daysBetween30US
    | DC30E360 -> DC30360.daysBetween30E
    | DC30E360ISDA -> DC30360.daysBetween30EISDA

