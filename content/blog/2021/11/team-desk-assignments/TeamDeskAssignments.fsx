#r "nuget: Flips, 2.4.6"

fsi.ShowDeclarationValues <- false

open Flips
open Flips.Types
open Flips.SliceMap


type TeamId = TeamId of int
type DeskId = DeskId of int
type PersonId = PersonId of int
type ClusterId = ClusterId of int

let rng = System.Random 123
let teamCount = 5
let personCount = 22
let clusterCount = 8
let deskCount = 30

let teamIds =
    [|1 .. teamCount|]
    |> Array.map TeamId

let personIds =
    [|1 .. personCount|]
    |> Array.map PersonId

let clusterIds =
    [|1 .. clusterCount|]
    |> Array.map ClusterId

let deskIds =
    [|1 .. deskCount|]
    |> Array.map DeskId

let teams =
    personIds
    |> Array.splitInto teamIds.Length
    |> Array.mapi (fun idx personIds ->
        let teamId = teamIds[idx]
        teamId, personIds
    ) |> readOnlyDict

let clusters =
    deskIds
    |> Array.splitInto clusterIds.Length
    |> Array.mapi (fun idx desks ->
        let clusterId = clusterIds[idx % clusterIds.Length]
        clusterId, Set desks
    ) |> readOnlyDict


let deskToCluster =
    clusters
    |> Seq.collect (fun (KeyValue (clusterId, deskIds)) -> 
        deskIds 
        |> Seq.map (fun deskId -> 
            deskId, clusterId))
    |> readOnlyDict

let currentPersonDeskAssignment =

    let randomDeskOrder =
        deskIds
        |> Array.sortBy (fun _ -> rng.NextDouble ())

    randomDeskOrder[0 .. personIds.Length - 1]
    |> Array.zip personIds


let personDeskAssignment =
    DecisionBuilder "PersonAssignment" {
        for p in personIds do
        for d in deskIds ->
            Boolean
    } |> SMap2


let teamClusterAssignment =
    DecisionBuilder "TeamClusterAssignment" {
        for t in teamIds do
        for c in clusterIds ->
            Boolean
    } |> SMap2

let eachPersonHasDeskConstraints =
    ConstraintBuilder "EachPersonHasDesk" {
        for p in personIds ->
            sum personDeskAssignment[p,  All] == 1.0
    }

let eachDeskOnlyOnceConstraints =
    ConstraintBuilder "EachDeskOnlyOnce" {
        for d in deskIds ->
            sum personDeskAssignment[All, d] <== 1.0
    }

let eachTeamHasClusterConstraints =
    ConstraintBuilder "EachTeamHasCluster" {
        for t in teamIds ->
            sum teamClusterAssignment[t, All] == 1.0
    }

let eachClusterOnlyOnceConstraints =
    ConstraintBuilder "EachClusterOnlyOnce" {
        for c in clusterIds ->
            sum teamClusterAssignment[All, c] <== 1.0
    }

let personCoLocated =
    DecisionBuilder "TeamCoLocated" {
        for t in teamIds do
        for c in clusterIds do
        for p in teams[t] ->
            Boolean
    } |> SMap3

let personCoLocatedConstraints =
    ConstraintBuilder "TeamPersonCoLocated" {
        for t in teamIds do
        for c in clusterIds do
        for p in teams[t] ->
            personCoLocated[t, c, p] <== sum personDeskAssignment[p, In clusters[c]]
    }

let teamCoLocatedConstraints =
    ConstraintBuilder "TeamCoLocated" {
        for t in teamIds do
        for c in clusterIds do
        for p in teams[t] ->
            personCoLocated[t, c, p] <== teamClusterAssignment[t, c]
    }

let colocationObjectiveExpr = sum personCoLocated

let coLocationObjective =
    Objective.create "MaximizeCoLocation" Maximize colocationObjectiveExpr

let maxRetentionExpr =
    seq {
        for (p, d) in currentPersonDeskAssignment ->
            1.0 * personDeskAssignment[p, d]
    } |> Seq.sum

let maxRetentionObjective =
    Objective.create "MaxRetention" Maximize maxRetentionExpr

let model =
    Model.create coLocationObjective
    |> Model.addObjective maxRetentionObjective
    |> Model.addConstraints eachPersonHasDeskConstraints
    |> Model.addConstraints eachDeskOnlyOnceConstraints
    |> Model.addConstraints eachTeamHasClusterConstraints
    |> Model.addConstraints eachClusterOnlyOnceConstraints
    |> Model.addConstraints personCoLocatedConstraints
    |> Model.addConstraints teamCoLocatedConstraints

let settings = Settings.basic

let result = Solver.solve settings model

match result with
| Optimal sln ->

    let personDeskAssignmentValues = Solution.getValues sln personDeskAssignment

    let selectedDeskAssignments =
        personDeskAssignmentValues
        |> Map.toSeq
        |> Seq.filter (fun (_, value) -> value = 1.0)
        |> Seq.map fst
        |> readOnlyDict


    let teamClusterAssignmentValues = Solution.getValues sln teamClusterAssignment

    let selectedTeamClusterAssignment =
        teamClusterAssignmentValues
        |> Map.toSeq
        |> Seq.filter (fun (_, value) -> value = 1.0)
        |> Seq.map fst

    printfn "Team/Cluster Assignments"
    for (teamId, clusterId) in selectedTeamClusterAssignment do
        printfn $"{teamId}"
        printfn $"{clusterId}"
        printfn "=== People ==="

        for personId in teams.[teamId] do
            let deskId = selectedDeskAssignments.[personId]
            let clusterId = deskToCluster[deskId]
            printfn $"{personId} | {deskId} | {clusterId}"


| _ -> printfn "Uh Oh"
