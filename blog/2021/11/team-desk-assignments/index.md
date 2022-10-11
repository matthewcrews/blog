# Team Desk Assignments


I was recently asked a question on the [Flips GitHub page](https://github.com/fslaborg/flips/issues/180) which I felt warranted a full blog post. It is an interesting problem that I have seen several variations on so I wanted to provide a more detailed model. The largest example of this problem I have seen is at Rocket Technology (formerly Quicken Loans) where they must assign desks to thousands of people across several buildings in downtown Detroit, MI.

You have a set of People that you need to assign desks to. Each person is a part of a Team, and you would rather that people sat with their Team. Each Desk is a part of a Cluster. A Cluster is a group of desks that are next to each other. Each Person is already assigned to a Desk. You want to come up with a seating assignment that maximizes the number of people who are sitting with their team while also minimizing the number of times that people must move from the desk they are sitting at currently.

## The Domain

Before we start putting our Mathematical Planning Model together, let's create simple types to describe our domain. Based on the word description of our problem we already have four types: `PersonId`, `TeamId`, `DeskId`, and `ClusterId`. For the sake of simplicity, we are going to model all of these as single-case DUs with an `int` as the value.

```fsharp
type TeamId = TeamId of int
type DeskId = DeskId of int
type PersonId = PersonId of int
type ClusterId = ClusterId of int
```

I don't have access to the original enquirers data set, so I need to create a synthetic data. I'll start by creating a new `System.Random` and give it an initial seed value of `123` so I can reproduce my results and define the number of teams, persons, clusters, and desks in my problem.

```fsharp
let rng = System.Random 123
let teamCount = 5
let personCount = 22
let clusterCount = 8
let deskCount = 30
```

Now I'll create some arrays of my data.

```fsharp
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
```

I need to separate the people into which team they belong to. F# has a handy function in the `Array` module which makes it easy to divide an array into evenly divided chunks called `Array.splitInto`. You give the `splitInto` function the number of chunks you want, and it evenly divides the input array into that many chunks. Let's now divide the people into which team they belong to.

```fsharp
let teams =
    personIds
    |> Array.splitInto teamIds.Length
    |> Array.mapi (fun idx personIds ->
        let teamId = teamIds[idx]
        teamId, personIds
    ) |> readOnlyDict
```

I now have a `IReadOnlyDictionary<TeamId, array<PersonId>>` which allows me to query which `PersonId` belong to a given `TeamId`.

I also want to divide the desks into which clusters they belong to. I'll use the same function as before to evenly divide them.

```fsharp
let clusters =
    deskIds
    |> Array.splitInto clusterIds.Length
    |> Array.mapi (fun idx desks ->
        let clusterId = clusterIds[idx % clusterIds.Length]
        clusterId, Set desks // Notice I'm creating a Set<DeskId>
    ) |> readOnlyDict
```

Now I have a `IReadOnlyDictionary<ClusterId, Set<DeskId>>`. I can look up a `ClusterId` and get a `Set` which contains all the `DeskId` that belong to that `ClusterId`. Notice, I put the `DeskId` into a `Set`, not an `array`. You will see why that matters later.

Later in my problem I will need to lookup which `ClusterId` that a `DeskId` belongs to so I go ahead and create an `IReadOnlyDictionary<DeskId, ClusterId>` from the same data.

```fsharp
let deskToCluster =
    clusters
    |> Seq.collect (fun (KeyValue (clusterId, deskIds)) -> 
        deskIds 
        |> Seq.map (fun deskId -> 
            deskId, clusterId))
    |> readOnlyDict
```

Each `PersonId` is already assigned to a desk. Since I don't have access to the real data, I'm just going to assign each `PersonId` to a random `DeskId` and store that in a `array<PersonId * DeskId>`.

```fsharp
let currentPersonDeskAssignment =

    let randomDeskOrder =
        deskIds
        |> Array.sortBy (fun _ -> rng.NextDouble ())

    randomDeskOrder[0 .. personIds.Length - 1]
    |> Array.zip personIds
```

## The Model

We now have a tiny domain and synthetic data. Let's being building our model! We start with creating the decision variables which represent assigning a given `PersonId` to a particular `DeskId`. We will use a `Boolean` decision variable. A value of `1` will represent `true`. `true` means that we do assign a given `PersonId` to a given `DeskId`. A value of `0` represents `false` which means we do NOT assign a `PersonId` to a given `DeskId`. We store these decision variables in a `SMap2<PersonId, DeskId, Decision>` for easy slicing later.

```fsharp
let personDeskAssignment =
    DecisionBuilder "PersonAssignment" {
        for p in personIds do
        for d in deskIds ->
            Boolean
    } |> SMap2
```

We now create a decision variable which represents assigning a `TeamId` to a `ClusterId`. The decision variable will be a `Boolean` where `1` means we do assign the `TeamId` to the `ClusterId` and `0` means we do not. We store these decisions in a `SMap2<TeamId, ClusterId, Decision>`.

```fsharp
let teamClusterAssignment =
    DecisionBuilder "TeamClusterAssignment" {
        for t in teamIds do
        for c in clusterIds ->
            Boolean
    } |> SMap2
```

Alright, now we can start creating constraints. First thing we need to do is ensure that each `PersonId` is assigned to exactly `1` desk.

```fsharp
let eachPersonHasDeskConstraints =
    ConstraintBuilder "EachPersonHasDesk" {
        for p in personIds ->
            sum personDeskAssignment[p,  All] == 1.0
    }
```

Notice that the comparison is `==` and not `<==`. An `==` means that the sum MUST equal `1.0`. If we had put `<==`, that would mean that the sum MAY equal `1.0` but it doesn't have to. This would allow the optimization to not ensure that everyone has a spot. That's not what we want though so we use `==` to ensure each `PersonId` will have a `DeskId` they are assigned to.

Next, we make sure that each `DeskId` is only assigned once.

```fsharp
let eachDeskOnlyOnceConstraints =
    ConstraintBuilder "EachDeskOnlyOnce" {
        for d in deskIds ->
            sum personDeskAssignment[All, d] <== 1.0
    }
```

Again, please notice the comparison that is being used here. Now we use `<==` instead of `==`. We are saying a `DeskId` MAY be assigned up to once, but no more. We are not requiring that each `DeskId` have a `PersonId` assigned to it. This is an important distinction.

Now we want to make sure that each `TeamId` is assigned a `ClusterId`.

```fsharp
let eachTeamHasClusterConstraints =
    ConstraintBuilder "EachTeamHasCluster" {
        for t in teamIds ->
            sum teamClusterAssignment[t, All] == 1.0
    }
```

Because we use `==` in the comparison we are ensuring that each `TeamId` is assigned a `ClusterId`.

We now must ensure that each `ClusterId` is assigned only once.

```fsharp
let eachClusterOnlyOnceConstraints =
    ConstraintBuilder "EachClusterOnlyOnce" {
        for c in clusterIds ->
            sum teamClusterAssignment[All, c] <== 1.0
    }
```

We are using `<==` which is saying that we may assign each `ClusterId` once but we don't have to.

### Team Co-Location

Now we get to the slightly complex part. This is where the original enquirer was stuck, and I completely understand why. Everything up to this point has been straightforward. Now we need to quantify the success of teams sitting together. To do this we are going to use something called Indicator Variables. Indicator Variables are used to indicate whether a certain condition is being met in a model or not. We are doing to use them to model whether a given `PersonId` is sitting with their assigned `TeamId`.

We will need to create a set of indicator variables for each `TeamId`, `ClusterId`, and `PersonId` combination.

```fsharp
let personCoLocated =
    DecisionBuilder "TeamCoLocated" {
        for t in teamIds do
        for c in clusterIds do
        for p in teams[t] ->
            Boolean
    } |> SMap3
```

We now have an `SMap3<TeamId, ClusterId, PersonId, Decision>` which we will use to model whether a `PersonId` is sitting with their `TeamId` at a given `ClusterId`.

You may have noticed that our assignment decisions were along different dimensions. `PersonId` are assigned to `DeskId` while `TeamId` are assigned to `ClusterId`. Now is when we bring together the dimensions of `DeskId` and `ClusterId`.

We want to maximize the number of times that a `PersonId` is assigned to the same `ClusterId` as their `TeamId`. Our objective function is going to be the sum of the `personCoLocated` decisions. To keep the optimizer from just turning all those values to `1.0` though we need to put constraints on them to make sure the necessary conditions are being met.

The first necessary condition is that a `PersonId` is assigned to a `DeskId` in the given `ClusterId`. Let's code that up.

```fsharp
let personCoLocatedConstraints =
    ConstraintBuilder "TeamPersonCoLocated" {
        for t in teamIds do
        for c in clusterIds do
        for p in teams[t] ->
            personCoLocated[t, c, p] <== sum personDeskAssignment[p, In clusters[c]]
    }
```

Here we are saying that for the solver to turn a given `personLocated` decision to `1.0`, at least one of the `personDeskAssignment` variables for the `PersonId` must be `1.0` as well. We are calculating that using this expression:

```fsharp
sum personDeskAssignment[p, In clusters[c]]
```

You'll see that the filter on the second dimensions is an `In` filter. The `In` filter takes a `Set` as an input which is why we had to store the values in the `cluster` collection as a `Set<DeskId>`.

Now we need to cover the second condition, the `TeamId` must also be assigned to the `ClusterId`.

```fsharp
let teamCoLocatedConstraints =
    ConstraintBuilder "TeamCoLocated" {
        for t in teamIds do
        for c in clusterIds do
        for p in teams[t] ->
            personCoLocated[t, c, p] <== teamClusterAssignment[t, c]
    }
```

Here we are saying that if you want to turn the value of the `personCoLocated` decision to `1.0`, you must have assigned the `TeamId` to that `ClusterId`.

## Putting it together

We can now put together all the components of our model. Let's create the `LinearExpression` which is our objective function.

```fsharp
let colocationObjectiveExpr = sum personCoLocated

let coLocationObjective =
    Objective.create "MaximizeCoLocation" Maximize colocationObjectiveExpr
```

We are saying that we want to maximize the number of times that a `PersonId` is assigned to the same `ClusterId` as the `TeamId` they are assigned to.

We have a secondary objective which is to minimize the number of times people are moved from their current seating assignment. To do this we create an expression which is the sum of whether a `PersonId` is assigned to the `DeskId` they are currently at.

```fsharp
let maxRetentionExpr =
    seq {
        for (p, d) in currentPersonDeskAssignment ->
            1.0 * personDeskAssignment[p, d]
    } |> Seq.sum

let maxRetentionObjective =
    Objective.create "MaxRetention" Maximize maxRetentionExpr
```

With our two objectives and various constraints we can compose our full model.

```fsharp
let model =
    Model.create coLocationObjective
    |> Model.addObjective maxRetentionObjective
    |> Model.addConstraints eachPersonHasDeskConstraints
    |> Model.addConstraints eachDeskOnlyOnceConstraints
    |> Model.addConstraints eachTeamHasClusterConstraints
    |> Model.addConstraints eachClusterOnlyOnceConstraints
    |> Model.addConstraints personCoLocatedConstraints
    |> Model.addConstraints teamCoLocatedConstraints
```

We aren't doing anything fancy so we will just use our default settings and attempt to solve.

```fsharp
let settings = Settings.basic

let result = Solver.solve settings model
```

Now let's create a simple function to print out the results if we find a solution. This will just extract which assignments the solver is recommending we use and print that out to a simple table in the console.

```fsharp
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
            let deskId = selectedDeskAssignments[personId]
            let clusterId = deskToCluster[deskId]
            printfn $"{personId} | {deskId} | {clusterId}"


| _ -> printfn "Uh Oh"
```

When we run all of this, we get the following result.

```terminal
Team/Cluster Assignments
TeamId 1
ClusterId 2
=== People ===
PersonId 1 | DeskId 5 | ClusterId 2
PersonId 2 | DeskId 4 | ClusterId 1
PersonId 3 | DeskId 7 | ClusterId 2
PersonId 4 | DeskId 8 | ClusterId 2
PersonId 5 | DeskId 6 | ClusterId 2
TeamId 2
ClusterId 4
=== People ===
PersonId 6 | DeskId 15 | ClusterId 4
PersonId 7 | DeskId 14 | ClusterId 4
PersonId 8 | DeskId 16 | ClusterId 4
PersonId 9 | DeskId 1 | ClusterId 1
PersonId 10 | DeskId 13 | ClusterId 4
TeamId 3
ClusterId 3
=== People ===
PersonId 11 | DeskId 9 | ClusterId 3
PersonId 12 | DeskId 10 | ClusterId 3
PersonId 13 | DeskId 11 | ClusterId 3
PersonId 14 | DeskId 12 | ClusterId 3
TeamId 4
ClusterId 5
=== People ===
PersonId 15 | DeskId 20 | ClusterId 5
PersonId 16 | DeskId 17 | ClusterId 5
PersonId 17 | DeskId 19 | ClusterId 5
PersonId 18 | DeskId 18 | ClusterId 5
TeamId 5
ClusterId 6
=== People ===
PersonId 19 | DeskId 23 | ClusterId 6
PersonId 20 | DeskId 24 | ClusterId 6
PersonId 21 | DeskId 22 | ClusterId 6
PersonId 22 | DeskId 21 | ClusterId 6
```

You will see that in most cases people are sitting with their teams but in some cases they are not. This is a product of a mismatch between the number of people on each team and the number of desks in each cluster. You can play with this model yourself [here](TeamDeskAssignments.fsx). Change the variables and see what results you get!

This was a fun problem to put together and I was grateful for the question. If you have a question about whether Mathematical Planning can help you and your team, please send me an email at matthewcrews@gmail.com if you have any questions and subscribe so you can stay on top new posts and products I am offering.

{{<subscribe>}}

