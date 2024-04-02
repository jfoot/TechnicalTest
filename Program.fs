module TechnicalTest.Main 
       
    open System
    open System.IO
    open System.Text.Json
    open TechnicalTest.InputParser
    open TechnicalTest.Types

    [<Literal>]
    let private successfulResponse = 0

    [<Literal>]
    let private unsuccessfulResponse = 1


    let private generateCache<'TKey when 'TKey : comparison> (selector: DateOnly -> 'TKey) (dates: Set<DateOnly>) : Map<'TKey, Set<DateOnly>> =
        dates
        |> Set.toList
        |> List.groupBy selector
        |> Map.ofList
        |> Map.map (fun _ -> Set.ofList)

    let generateSpecifiedDateOfMonthCache = generateCache _.Day
    let generateSpecifiedDayOfWeekCache = generateCache _.DayOfWeek


    let generateDateSequence (startDate: DateOnly) (endDate: DateOnly) : Set<DateOnly>  =
        Seq.initInfinite startDate.AddDays
        |> Seq.takeWhile (fun date -> date <= endDate)
        |> Set.ofSeq
        
    let createEmptyDateMap (dates: Set<DateOnly>) : Map<DateOnly, Set<CustomerId>> =
        dates
        |> Set.toList
        |> List.map (fun date -> (date, Set.empty))
        |> Map.ofList
       
    let insertCustomerIntoMap (customerName: CustomerId) (correspondingDates: Set<DateOnly>) (correspondingMap: Map<DateOnly, Set<CustomerId>>) : Map<DateOnly, Set<CustomerId>> =
        let updateKeyInMap (date : DateOnly) (map: Map<DateOnly, Set<string>>) =
            Map.change date (function
                            | Some valueSet -> Some (Set.add customerName valueSet)
                            | None -> Some (Set.singleton customerName)) map

        correspondingDates
        |> Set.fold (fun acc date -> updateKeyInMap date acc) correspondingMap
       
       
    let printResults (results: Map<DateOnly, Set<CustomerId>>) =
          results
          |> Map.iter (fun date customers -> 
                let formatDateString (date: DateOnly) : string =
                   let dateTime: DateTime = date.ToDateTime TimeOnly.MinValue
                   dateTime.ToString("ddd dd-MMMM-yyyy")
                                     
                let formatCustomers (customers: Set<CustomerId>) : string =
                    customers
                    |> String.concat ", "
            
                printfn $"{formatDateString date} {formatCustomers customers}"
          )
          
    let printAndSaveResults (results: Map<DateOnly, Set<CustomerId>>) =
         printResults results
         let jsonOptions = JsonSerializerOptions (JsonSerializerDefaults.General, WriteIndented = true)
         File.WriteAllText ("output.json", JsonSerializer.Serialize (results, jsonOptions))
       
   
    let generateCustomerPreference (customers: Customer List) (dateRanges: Set<DateOnly>) : Map<DateOnly, Set<CustomerId>>  =
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
        let correspondentMap = createEmptyDateMap dateRanges
        
        List.fold (fun map customer ->
                       let datesOfCorrespondents =                      
                             match customer.MarketingPreference with
                             | OnSpecifiedDateOfMonth day -> dateOfMonthCache[day]
                             | OnSpecifiedDayOfWeek weekDays -> weekDays |> Set.map (fun day -> dayOfWeekCache[day]) |> Set.unionMany
                             | EveryDay -> dateRanges
                             | Never -> Set.empty
                       
                       insertCustomerIntoMap customer.Name datesOfCorrespondents map
                   ) correspondentMap customers
    
    [<EntryPoint>]
    let main argv =
       if argv.Length < 2 then
            printfn "Error: Input arguments missing, expected start date (dd-MM-yyyy) and input data path."
            exit unsuccessfulResponse
       
       
       match parseDateRange argv[0], generateCustomerData argv[1] with
       | Ok (startDate, endDate), Ok customers ->
           generateDateSequence startDate endDate
           |> generateCustomerPreference customers
           |> printAndSaveResults

           successfulResponse
       | Error errorMessage, Error errorMessage2 -> 
            printfn $"Error: {errorMessage} & {errorMessage2}"
            exit unsuccessfulResponse
       | Error errorMessage, _ | _, Error errorMessage ->
            printfn $"Error: {errorMessage}"
            exit unsuccessfulResponse
     