module Main 
       
    open System
    open System.IO
    open System.Text.Json
    open TechnicalTest.InputParser
    open TechnicalTest.Types

    [<Literal>]
    let private successfulResponse = 0

    [<Literal>]
    let private unsuccessfulResponse = 1


    let generateCache<'TKey when 'TKey : comparison> (selector: DateOnly -> 'TKey) (dates: DateOnly list) : Map<'TKey, Set<DateOnly>> =
        dates
        |> List.groupBy selector
        |> Map.ofList
        |> Map.map (fun _ -> Set.ofList)

    let generateSpecifiedDateOfMonthCache = generateCache _.Day
    let generateSpecifiedDayOfWeekCache = generateCache _.DayOfWeek


    let generateDateSequence (startDate: DateOnly) (endDate: DateOnly) : DateOnly list  =
        Seq.initInfinite startDate.AddDays
        |> Seq.takeWhile (fun date -> date <= endDate)
        |> Seq.toList
        
    let createEmptyDateMap (dates: DateOnly list) : Map<DateOnly, Set<string>> =
        dates
        |> List.map (fun date -> (date, Set.empty))
        |> Map.ofList
       
    let insertCustomerIntoMap (customerName : string) (correspondingDates: Set<DateOnly>) (correspondingMap: Map<DateOnly, Set<string>>) : Map<DateOnly, Set<string>> =
        let updateKeyInMap (date : DateOnly) (map: Map<DateOnly, Set<string>>) =
            Map.change date (function
                            | Some valueSet -> Some (Set.add customerName valueSet)
                            | None -> Some (Set.singleton customerName)) map

        correspondingDates
        |> Set.fold (fun acc date -> updateKeyInMap date acc) correspondingMap
       
       
    let printResults results =
          results
          |> Map.iter (fun date customers -> 
                let formatDateString (date: DateOnly) : string =
                   let dateTime: DateTime = date.ToDateTime TimeOnly.MinValue
                   dateTime.ToString("ddd dd-MMMM-yyyy")
                                     
                let formatCustomers (customers: Set<string>) : string =
                    customers
                    |> String.concat ", "
            
                printfn $"{formatDateString date} {formatCustomers customers}"
          )
       
  
    [<EntryPoint>]
    let main argv =
       if argv.Length < 2 then
            printfn "Error: Input arguments missing, expected start date (dd-MM-yyyy) and input data path."
            exit unsuccessfulResponse
       
       
       match parseDateRange argv[0], generateCustomerData argv[1] with
       | Ok (startDate, endDate), Ok customers ->
           let dateRanges = generateDateSequence startDate endDate
           let correspondentMap = createEmptyDateMap dateRanges
     
           
           // I build a cache of all possible dates and day values, as there is a known fixed number (90 days)
           // worth of values - which is small enough to compute all values to then look-up.
           // 
           // Where as, there could be an infinite number of customers, which could require recomputing
           // values multiple times. This should ensure performance with large customer input sizes.
           // ---
           // I could have built up the cache while going through the 'fold' but decided against it for
           // better code readability.
           let dateOfMonthCache = generateSpecifiedDateOfMonthCache dateRanges
           let dayOfWeekCache = generateSpecifiedDayOfWeekCache dateRanges
           let results = 
                List.fold (fun map customer ->
                               let datesOfCorrespondents =                      
                                     match customer.MarketingPreference with
                                     | OnSpecifiedDateOfMonth day -> dateOfMonthCache[day]
                                     | OnSpecifiedDayOfWeek weekDays -> weekDays |> Set.map (fun day -> dayOfWeekCache[day]) |> Set.unionMany
                                     | EveryDay -> dateRanges |> Set.ofList
                                     | Never -> Set.empty
                               
                               insertCustomerIntoMap customer.Name datesOfCorrespondents map
                           ) correspondentMap customers
                   
           printResults results
           File.WriteAllText ("output.json", JsonSerializer.Serialize (results, JsonSerializerOptions (JsonSerializerDefaults.General, WriteIndented = true)))
           successfulResponse
       | Error errorMessage, Error errorMessage2 -> 
            printfn $"Error: {errorMessage} & {errorMessage2}"
            exit unsuccessfulResponse
       | Error errorMessage, _ | _, Error errorMessage ->
            printfn $"Error: {errorMessage}"
            exit unsuccessfulResponse
     