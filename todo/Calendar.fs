namespace Marreco.Finance.Core
open System
open Utilities

module Calendar =

    let private periodCalculation fn d1 d2 = 
       if (d2 < d1) then -(fn d2 d1)
       else fn d1 d2

    // Units of measure for time
    [<Measure>] type years;
    [<Measure>] type months;
    [<Measure>] type days;
    [<Measure>] type weeks;


    type Period =
    | Days of int<days>
    | Months of int<months>
    | Years of float<years>

    let years (y:float<1>) = y * 1.0<years> |> Years


    type Frequency  = 
    | Continous
    | TimesPerDay of int</days>
    | TimesPerMonth of int</months>
    | TimesPerYear of int</years>

    let Annually = 1</years> |> TimesPerYear 
    let Daily = 1</days> |> TimesPerDay
    let Monthly = 1</days> |> TimesPerDay
    let SemiAnnally = 2</years> |> TimesPerYear
    let Quarterly = 4</years> |> TimesPerYear


    //Active patterns for datetime
    let (|Date|) (date:DateTime) = (date.Year, date.Month, date.Day)

    let (|EndOfMonth|StartOfMonth|MiddleOfMonth|) (date:DateTime) = 
        match date with
        | Date(_, m , 1) -> StartOfMonth(m) 
        | d when (DateTime(d.AddMonths(1).Year, d.AddMonths(1).Month, 1).AddDays(-1.0)) = d 
                         -> EndOfMonth(d.Month) 
        | Date(_, m , _) -> MiddleOfMonth(m)

    let (|Weekday|Weekend|) (date:DateTime) = 
        match date.DayOfWeek with
        | DayOfWeek.Sunday
        | DayOfWeek.Saturday -> Weekend
        | _                  -> Weekday


    let weekendDaysBetween  = 
        let countWeekends (_startDate:DateTime) (_endDate:DateTime) = 
            let startDate = _startDate.Date;
            let endDate = _endDate.Date;
            let days = endDate.Subtract(startDate).Days * 1<days> ;
            let fullWeeks =days/(7<days/weeks>);
            let startDayOfWeek = startDate.DayOfWeek;
            let result = fullWeeks * 2<days/weeks>
                          + match endDate.DayOfWeek with
                            | x when x < startDayOfWeek -> 2<days>
                            | DayOfWeek.Saturday -> 1<days>
                            | _ -> 0<days>
                          + match startDayOfWeek with 
                            | DayOfWeek.Sunday -> 1<days>
                            | _ -> 0<days>;
            result;                
        countWeekends |> orderParams




        
    [<StructuredFormatDisplay("{Name}")>]
    type Calendar (name:string, holidays:List<DateTime>) = 
        let rec expandWorkdayCount holidayLst acc count (date:DateTime) = 
            match (date,holidayLst) with
            | (_, []) -> acc |> List.rev
            | (d, h::t) when d = h ->printfn "1a"; expandWorkdayCount t (count::acc) count (date.AddDays(1.0))
            | (Weekend, hs) -> printfn "1b"; expandWorkdayCount hs (count::acc) count (date.AddDays(1.0))
            | (_, h) -> printfn "1c"; expandWorkdayCount h (((count+1)::acc)) (count+1) (date.AddDays(1.0))
            | _ -> acc |> List.rev
        let firstDay = match holidays with | h::t -> h | _ -> DateTime.MaxValue
        let lastDay = holidays |> List.last;
        let workdayCount = expandWorkdayCount holidays [] 0 firstDay
        member x.NetworkDays (startDate:DateTime) (endDate:DateTime)  = 
            // calculo sem calendario
            let workdaysBetween (_startDate:DateTime) (_endDate:DateTime) = 
                let startDate = Seq.initInfinite (float >> _startDate.AddDays) |> Seq.find (function |Weekend -> false | _ -> true)
                let endDate = Seq.initInfinite (((*)-1) >> float >> _endDate.AddDays) |> Seq.find (function |Weekend -> false | _ -> true)
                let actualDays = endDate.Subtract(startDate).TotalDays 
                let weekCount = (actualDays |> int) / 7
                match (int startDate.DayOfWeek, int endDate.DayOfWeek) with
                | (w1, w2) when w1 > w2 -> w2 - w1 + 5 + weekCount * 5
                | (w1, w2) -> w2 - w1 + weekCount * 5
            let workdaysBefore = if (startDate < firstDay) then workdaysBetween startDate firstDay else 0
            let workdaysAfter = if (endDate > lastDay) then workdaysBetween lastDay endDate else 0
            let workdaysBetween = workdayCount.[int ((min endDate lastDay).Subtract(firstDay).TotalDays)] - workdayCount.[int ((max startDate firstDay).Subtract(firstDay).TotalDays)]
            (workdaysBefore + workdaysAfter + workdaysBetween) * 1<days>
        member x.Holidays = holidays

        member x.Name = name


    type DayCountConvention = 
        | DCWD252 of Calendar
        | DC30E360
        | DC30360US
        | DCACT360
        | DCACT365
        | DCACTACTISDA

    //Subtract 2 days
    let actualDaysBetween (date1:DateTime) (date2:DateTime) = (int (date2.Subtract(date1).TotalDays)) * 1<days>

    //Daycount using convention
    let rec daysBetween dayCountConvention (date1:DateTime) (date2:DateTime) = 
        match dayCountConvention with
        | DCWD252 calendar  -> calendar.NetworkDays date1 date2
        | DC30E360          -> match (date1, date2) with
                                | (Date (y1, m1 , 31), _) -> daysBetween dayCountConvention (DateTime(y1, m1, 30)) date2
                                | (_, Date (y2, m2 , 31)) -> daysBetween dayCountConvention date1 (DateTime(y2, m2, 30))
                                | _ -> actualDaysBetween date1 date2
        | DC30360US         -> match (date1, date2) with
                               | (EndOfMonth 2, EndOfMonth 2) -> daysBetween dayCountConvention date1 (date2.AddDays(float (30-date2.Day)))
                               | (EndOfMonth 2, _) -> daysBetween dayCountConvention (date1.AddDays(float (30-date2.Day))) date2
                               | (Date(_, _, 30), Date (y2, m2, 31)) 
                               | (Date(_, _, 31), Date (y2, m2, 31)) -> daysBetween dayCountConvention date1 (DateTime(y2, m2, 30))
                               | (Date(y1, m1, 31), _) -> daysBetween dayCountConvention (DateTime(y1, m1, 30)) date2
                               | _ -> actualDaysBetween date1 date2
        | DCACT360 
        | DCACT365 
        | DCACTACTISDA      -> actualDaysBetween date1 date2


    let yearsBetween dayCountConvention (date1:DateTime) (date2:DateTime) = 
        let days = daysBetween dayCountConvention date1 date2 |> intToFloat
        match dayCountConvention with
        | DCWD252 _     -> days / (252.0<days/years>)
        | DC30E360      
        | DC30360US     
        | DCACT360      -> days / (360.0<days/years>)
        | DCACT365      -> days / (365.0<days/years>)
        | DCACTACTISDA  ->  Seq.unfold (fun dt -> match dt with
                                                  | _ when dt >= date2                 ->  None
                                                  | Date(y, _, _)  when y = date2.Year ->  Some((dt,date2), date2)
                                                  | Date(y, _, _)                      ->  let firstNextYear = DateTime(y+1, 1, 1); 
                                                                                           Some((dt, firstNextYear), firstNextYear)) date1 
                            |> Seq.sumBy (fun (d1, d2)-> 
                                    let daysInYear (dt:DateTime) = (if (DateTime.IsLeapYear(dt.Year)) then 366.0<days/years> else 365.0<days/years>)
                                    (intToFloat (actualDaysBetween d1 d2)) / (daysInYear d1))


