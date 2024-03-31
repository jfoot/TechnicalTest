module TechnicalTest.Types

    open System
    open System.Text.Json
    open System.Text.Json.Serialization 
    

    [<JsonConverter(typeof<MarketingPreferencesConverter>)>]
    type MarketingPreferences =
       | OnSpecifiedDateOfMonth of int
       | OnSpecifiedDayOfWeek of Set<DayOfWeek>
       | EveryDay
       | Never
       
    and MarketingPreferencesConverter() =
        inherit JsonConverter<MarketingPreferences>()

        override this.Write (writer : Utf8JsonWriter, value : MarketingPreferences, options : JsonSerializerOptions) =
            match value with
            | OnSpecifiedDateOfMonth day -> writer.WriteStringValue($"OnSpecifiedDateOfMonth {day}")
            | OnSpecifiedDayOfWeek days -> writer.WriteStringValue($"OnSpecifiedDayOfWeek {JsonSerializer.Serialize(days)}")
            | EveryDay -> writer.WriteStringValue("EveryDay")
            | Never -> writer.WriteStringValue("Never")

        override this.Read (reader, typeToConvert : System.Type, options : JsonSerializerOptions) =
            let stringValue = reader.GetString()
            match stringValue with
            | "EveryDay" -> EveryDay
            | "Never" -> Never
            | _ ->
                let parts = stringValue.Split(' ')
                if parts.Length <> 2 then
                    failwith $"Missing required parameters {stringValue}"
                match parts[0] with
                | "OnSpecifiedDateOfMonth" -> OnSpecifiedDateOfMonth (int parts[1])
                | "OnSpecifiedDayOfWeek" ->
                    let days = JsonSerializer.Deserialize<Set<DayOfWeek>>(parts[1])
                    OnSpecifiedDayOfWeek (Set.ofSeq days)
                | _ -> failwith "Invalid MarketingPreferences value in JSON"
        
    type Customer =
       {
          Name: String
          MarketingPreference : MarketingPreferences
       }