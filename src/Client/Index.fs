module Index

open Elmish

open Feliz
open Feliz.Bulma
open Feliz.Recharts
open Feliz.PigeonMaps
open Elmish.SweetAlert

open Fable.Remoting.Client
open Shared
open Bulma
open type Html
open type Bulma

module List =
    let removeItem idx xs =
        xs |> List.indexed |> List.filter (fun (i,v) -> i <> idx) |> List.map snd

    let setItem idx v xs =
        let n = List.length xs
        if idx > n then 
            failwith "invalid set"
        elif idx = n then 
            List.append xs [v]
        else
            xs |> List.indexed |> List.map (fun (i,v2) -> if i = idx then v else v2) 

    let filteri f xs =
        xs |> List.indexed |> List.filter (fun (i,x) -> f i x) |> List.map snd

type ServerResponse =
    { Location: LocationResponse
      Weather: WeatherResponse }

type ServerState =
    | Idle
    | Loading
    | ServerError of string

type Destination =
    { Text: string
      ValidationError: string option
      ServerState: ServerState
      ServerResponse: ServerResponse option }

    static member Empty =
        { Text = ""
          ServerResponse = None
          ValidationError = None
          ServerState = Idle }


/// The overall data model driving the view.
type Model =
    { Destinations: Destination list }

    member model.GetDestination idx =
        match List.tryItem idx model.Destinations with
        | None -> Destination.Empty
        | Some stopInfo -> stopInfo

    member model.SetDestination idx stopInfo =
        let destinations =
            model.Destinations
            |> List.setItem idx stopInfo 
            |> List.filter (fun v -> v.Text <> "")
        { model with Destinations = destinations }

    member model.UpdateDestination idx f =
        model.SetDestination idx (f model.GetDestination idx)

    member model.RemoveDestination idx =
        let destinations =
            model.Destinations
            |> List.removeItem idx  
        { model with Destinations = destinations }

/// Which stop in the trip is being referred to?
type DestinationIndex = int

/// The different types of messages in the system.
type Msg =
    | TextChanged of DestinationIndex * string
    | GetDestination of DestinationIndex
    | GotDestination of DestinationIndex * ServerResponse
    | ErrorMsg of DestinationIndex * exn
    (* Task 4.1 Add a new message RemoveDestination carrying a destination number *)
#if SOLVED
#else
    | RemoveDestination of DestinationIndex
#endif


/// The init function is called to start the message pump with an initial view.
let init () =
    let model = { Destinations = [] }
    model, Cmd.none

let dojoApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IDojoApi>

let getResponse stopText = async {
    let! location = dojoApi.GetLocation stopText
    let! weather = dojoApi.GetWeather stopText
    return
        { Location = location
          Weather = weather }
}

let country = "NL"

/// The update function knows how to update the model given a message.
let update msg (model: Model) =
    match msg with
    | GetDestination idx->
        let stopInfo = model.GetDestination idx
        match stopInfo.ValidationError with
        | None -> 
            let model = model.SetDestination idx  { stopInfo with ServerState = Loading }
            model, Cmd.OfAsync.either getResponse stopInfo.Text (fun r -> GotDestination(idx, r)) (fun msg -> ErrorMsg(idx, msg))
        | Some _ -> 
            model, Cmd.none

    | GotDestination (idx, response) ->
        let stopInfo = model.GetDestination idx
        let stopInfo = 
            { stopInfo  with
                ValidationError = None
                ServerResponse = Some response
                ServerState = Idle }
        let model = model.SetDestination idx stopInfo
        model, Cmd.none

    (* Task 4.2 Process the message RemoveDestination carrying a destination number *)
    (*          You can call model.RemoveDestination to generate a new model *)
    (*          with the element removed *)
#if SOLVED
#else
    | RemoveDestination idx ->
        let model = model.RemoveDestination idx
        model, Cmd.none
#endif

    | TextChanged (idx, p) ->
        let stopInfo = model.GetDestination idx
        let stopInfo = 
            { stopInfo with
                Text = p
                ValidationError = 
                    if p = "" || Validation.isValidPostcode country p then
                        None
                    else
                        Some "invalid postcode" }
        let model = model.SetDestination idx stopInfo
        model, Cmd.none

    | ErrorMsg (idx, e) ->
        let stopInfo = model.GetDestination idx
        let stopInfo = 
            { stopInfo with
                ServerState = ServerError e.Message }
        let errorAlert =
            SimpleAlert(e.Message)
                .Title("Try another postcode")
                .Type(AlertType.Error)
        let model = model.SetDestination idx stopInfo
        model, SweetAlert.Run errorAlert

let widget (title: string) (content: ReactElement list) =
    box [
        prop.children [
            subtitle title
            yield! content
        ]
    ]


let navbar =
    navbar [
        color.isInfo
        prop.children [
            navbarBrand.a [
                prop.href "https://safe-stack.github.io/docs/"
                prop.target "_"
                prop.children [
                    navbarItem.div [
                        title [
                            icon [
                                icon.isLarge
                                prop.style [ style.color.white; style.transform.scaleX -1 ]
                                prop.children [
                                    i [ prop.className "fas fa-unlock-alt" ]
                                ]
                            ]
                            span [
                                prop.style [ style.color.white ]
                                prop.text "Guido's Trip to Europe"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let makeMarker (latitude, longitude)  =
    PigeonMaps.marker [
        marker.anchor (latitude, longitude)
        marker.render (fun marker -> [
            i [
                if marker.hovered
                then prop.style [ style.color.red; style.cursor.pointer ]
                prop.className [ "fa"; "fa-map-marker"; "fa-2x" ]
            ]
        ])
    ]

let mapWidget (lr:LocationResponse) =
    let latLong = (lr.Location.LatLong.Latitude, lr.Location.LatLong.Longitude)
    PigeonMaps.map [
        (* Task 2.2 MAP: Set the center of the map using map.center *)
        (* supply the lat/long value as input. These come from the LocationResponse *)
#if SOLVED
#else
        map.center (lr.Location.LatLong.Latitude, lr.Location.LatLong.Longitude)
#endif

        (* Task 2.3 MAP: Update the Zoom to 15. *)
#if SOLVED
        map.zoom 12
#else
        map.zoom 15
#endif
        map.height 500
        map.markers [
            (* Task 2.4 MAP: Create a marker for the map. Use the makeMarker function above. *)
#if SOLVED
#else
            makeMarker (lr.Location.LatLong.Latitude, lr.Location.LatLong.Longitude)
#endif
        ]
    ]

let weatherDisplay (wr: WeatherResponse) =
    div [
        image [
            prop.children [
                img [
                    prop.style [ style.height 100]
                    prop.src (sprintf "https://www.metaweather.com/static/img/weather/%s.svg" wr.WeatherType.Abbreviation) ]
                ]
            ]
        table [
            table.isNarrow
            table.isFullWidth
            prop.style [ style.marginTop 37 ]
            prop.children [
                tbody [
                    tr [
                        th "Temp"
                        (* Task 3.4 Fill in the temperature, the right number is *)
                        (*          available in the WeatherResponse *)
#if SOLVED
                        td $"%.1f{3.1415}"
#else
                        td $"%.1f{wr.AverageTemperature}"
#endif
                    ]
                ]
            ]
        ]
    ]

let locationDisplay (lr: LocationResponse) =
    table [
        table.isNarrow
        table.isFullWidth
        prop.children [
            tbody [
                tr [
                    th "Region"
                    (* Task 1.2 The region has "TODO".  *)
                    (*          Fill in the region, founf in the LocationResponse *)
                    (*          Replace the string "TODO" with lr.Location and  *)
                    (*          then hit "." to look for the Region *)
#if SOLVED
                    td "TODO"
#else
                    td lr.Location.Region
#endif
                ]
                tr [
                    (* Task 1.3 Heathrow is in the wrong country. *)
                    (*          Change Heathrow to Schiphol *)
                    (*          Find the definition of 'heathrow' and change to the right value for Schipol *)
                    (*          If you like, search for DistanceToAirport and see how it's calculated *)
#if SOLVED
                    th "Distance to Heathrow"
#else
                    th "Distance to Schiphol"
#endif
                    td $"%.2f{lr.DistanceToAirport}km" 
                ]
            ]
        ]
    ]

let adjective idx =
    match idx+1 with 
    | 1 -> "First"
#if SOLVED
    (* Task 1.1 The text is wrong - 2th, 3th etc. *)
    (*          Add entries for Second, Third, Fourth, Fifth, 6th etc. *)
    | n -> string n + "st"
#else
    | 2 -> "Second"
    | 3 -> "Third"
    | 4 -> "Fourth"
    | 5 -> "Fifth"
    | n -> string n + "th"
#endif

let destinationEntrySection idx (destination: Destination) dispatch =
    box [
        
        label $"{adjective idx} Stop"
        
        field.div  [
            field.hasAddons
            prop.children [
                control.div [
                    control.hasIconsLeft
                    prop.style [ style.width (length.percent 100)]
                    control.hasIconsRight
                    prop.children [
                        // Show the input box
                        input.text [
                            if destination.ValidationError.IsSome then color.isDanger else color.isInfo
                            prop.placeholder "Ex: EC2A 4NE"
                            prop.style [ style.textTransform.uppercase ]
                            prop.value destination.Text
                            prop.onChange (fun text -> TextChanged(idx, text) |> dispatch)
                        ]

                        // Show the home logo in the input box
                        icon [
                            icon.isLeft
                            prop.children [
                                i [ prop.className "fas fa-home"]
                            ]
                        ]

                        // Show the validation tick/cross
                        match destination.ValidationError with
                        | Some _ ->
                            icon [
                                icon.isRight
                                prop.children [
                                    i [ prop.className "fas fa-times"]
                                ]
                            ]
                        | None when destination.Text = "" -> ()
                        | None -> 
                            icon [
                                icon.isRight
                                prop.children [
                                    i [ prop.className "fas fa-check"]
                                ]
                            ]

                        // Show the validation error (if any)
                        help [
                            if destination.ValidationError.IsNone then color.isPrimary else color.isDanger
                            prop.text (destination.ValidationError |> Option.defaultValue "")
                        ]
                    ]
                ]

                // Show the submit button for this entry
                control.div [
                    button.a [
                        color.isInfo
                        prop.onClick (fun _ -> dispatch (GetDestination idx))
                        prop.disabled (destination.Text = "" || destination.ValidationError.IsSome)
                        if (destination.ServerState = Loading) then button.isLoading
                        prop.text "Fetch"
                    ]
                ]
                (* Task 4.3 Add a trash icon by uncommenting the code below *)

#if SOLVED
#else
                control.div [
                    button.a [
                        prop.children [
                            icon [
                                icon.isRight
                                prop.children [
                                    i [ prop.className "fas fa-trash"]
                                ]
                            ]
                        ]
                        (* Task 4.4 Add an onClick property that dispatches the *)
                        (* RemoveDestination message *)
#if SOLVED
#else
                        prop.onClick (fun _ -> dispatch (RemoveDestination idx)) 
#endif

                    ]
                ]
#endif
            ]
        ]
    ]

let destinationInfoSection idx (model: Destination) =
    [
        // Don't show the report if there is a server error
        match model.ServerState with 
        | ServerError _ -> ()
        | _ -> 
        // Don't show the report if it's not available
        match model.ServerResponse with
        | None -> ()
        | Some response ->
         
        section [
            prop.children [
                let title = $"{adjective idx} Stop - {response.Location.Location.Town}"
                widget title  [
                    columns [
                        column [
                            column.isThreeFifths
                            prop.children [
                                locationDisplay response.Location
                            ]
                        ]
                        (* Task 3.3 Add a second column containing the weather widget *)
                        (*          Create this using weatherDisplay, which takes a WeatherResponce *)
                        (*          This can be found in the overall server response *)
#if SOLVED
#else
                        column [
                            weatherDisplay response.Weather
                        ]
#endif
                    ]
                    (* Task 2.1 Add the map widget *) 
                    (*          This is created using mapWidget which takes a LocationResponse *)
                    (*          This can be found in the overall server response *)
#if SOLVED
#else
                    mapWidget response.Location
#endif
                ]
            ]
        ]
    ]

/// The view function knows how to render the UI given a model, as well as to dispatch new messages based on user actions.
let view (model:Model) dispatch =
    let destinations = model.Destinations
    div [
        prop.style [ style.backgroundColor "#eeeeee57"; style.minHeight (length.vh 100) ]
        prop.children [
            navbar

            section [
                for (i, destination) in List.indexed destinations do
                    destinationEntrySection i destination dispatch

                // Always show at least one empty section so we can add a stop
                destinationEntrySection destinations.Length Destination.Empty dispatch
            ]

            for (i, destination) in List.indexed destinations do
                yield! destinationInfoSection i destination
        ]
    ]


