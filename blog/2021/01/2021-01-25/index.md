# Scheduling Jobs for Maximum Efficiency - Part 1


{{< youtube 2n7nAyoEzGs >}}

I was recently having a discussion with a friend when they brought up a new problem they were looking into. He asked me if it was a good candidate for Mathematical Planning and I said, "Absolutely!" I am abstracting away the specific domain, but this is the essence of the problem.

There are a set of machines which can process jobs. The jobs are of different types and sizes. There are three job-types: A, B, and C. Each machine has different job-types that it can process. Some machines can process any job-type while other machines can only work on one or two. At the beginning of the day, we are given a set of jobs to assign to the machines. We want to assign jobs to machines such that a) the machines are evenly loaded and b) we minimize the number of different job-types each machine must process.

An ideal plan would have each machine with the same amount of work and only processing a single job-type. The reason we want a machine to only process a single job-type is to minimize the waste associated with changing between job-types. Switching between job-types is fast, it just creates unwanted waste. Let's start with creating a small domain model and generate some example data.

> **Note**: You can find the full code example for this post [here](https://github.com/matthewcrews/modelmondays/blob/main/2021-01-22-MachineAllocation/2021-01-22-MachineAllocation.fsx)

## A Domain for Job Assignments

Based on the description there are some clear types that we need to define: `Machine`, `Job`, and `JobType`.

```fsharp
[<RequireQualifiedAccess>]
type JobType =
    | A
    | B
    | C

type Job = {
    Id : int
    JobType : JobType
    Size : float
} with
    override this.ToString () =
        $"Job_{this.Id}"

type Machine = {
    Id : int
    JobTypes : Set<JobType>
} with
    override this.ToString () =
        $"Machine_{this.Id}"
```

I like to override the `ToString` method because the `ConstraintBuilder` and `DecisionBuilder` in `Flips` use `ToString` for the naming of constraints and decisions. `JobType` is a straightforward Discriminated Union with three different cases `A`, `B`, and `C`. `Job` has an `Id` field for identifying a particular `Job`, a `JobType` which describes the type of job that it is, and the `Size`.

The `Machine` type has an `Id` field and a `JobTypes` field. The `JobTypes` field is a `Set` of `JobType`. This represents the jobs the `Machine` can process.

We now want to setup some data for us to be able to play with. These will be the parameters which will help us generate random data for us to work with.

```fsharp
// Set of JobTypes for iterating over and sampling from
let jobTypes = 
    [|
        JobType.A
        JobType.B
        JobType.C
    |]

// Some theoretical JobTypeSets to be used in generating
// random Machines
let jobTypeSets =
    [|
        Set jobTypes
        Set jobTypes.[1..]
        Set jobTypes.[..1]
    |]

// Setting up parameters for the example
let rng = System.Random(123)
let numberOfJobs = 20
let numberOfMachines = 5
let minJobSize = 1
let maxJobSize = 3
let maxWorkDifference = 2.0
```

`jobTypes` is an `Array` which holds each of the possible `JobType` cases. We will use this to create random jobs. The `jobTypeSets` value is an `Array` of `Set<JobType>`. These are the possible values for the `JobTypes` field of `Machine` that we will use for generating random machines. For this example, we will have 20 jobs and 5 machines to assign them to. `minJobSize` will control how small a job can be and `maxJobSize` will determine how large. The `maxWorkDifference` parameter will determine how different the loading of machines that we will allow.

We now create some convenience functions for generating random data. We will also add a function for looking up a key in a `Map` but returning a default value when the key is not present.

```fsharp
let randomJobSize (rng: System.Random) =
    rng.Next(minJobSize, maxJobSize)
    |> float

let randomJobType (rng: System.Random) =
    jobTypes.[rng.Next(0, jobTypes.Length)]

let randomJobTypeSet (rng: System.Random) =
    jobTypeSets.[rng.Next(0, jobTypeSets.Length)]

module Map =

    // Useful when you want to look up a key in a Map but you want it to provide
    // a default value if the key is missing
    let tryFindDefault (key: 'a) (defaultValue: 'v) (m: Map<'a, 'v>) =
        match Map.tryFind key m with
        | Some v -> v
        | None -> defaultValue
```

We can now generate a random set of jobs and machines to work with.

```fsharp
// Create some examples jobs
let jobs =
    [1..numberOfJobs]
    |> List.map (fun id -> { 
        Id = id
        JobType = randomJobType rng
        Length = randomJobSize rng 
    })

// Create some test machines
let machines =
    [1..numberOfMachines]
    |> List.map (fun id -> {
        Id = id
        JobTypes = randomJobTypeSet rng
    })
```

## Formulating the Problem

Now that we have some data to work with, we can get to formulating our model. Let's go ahead and open `Flips`. I love working with VS Code, Ionide, and `.fsx` files for this kind of exploration. The new `#r "nuget: <library name>"` syntax for using Nuget packages has been a game changer.

```fsharp
#r "nuget: Flips"

open Flips
open Flips.Types
open Flips.SliceMap
```

We want to create a `Map` where they key is a `JobType` and the value is a list of `Job` that are of that type. This will make it easy for us to lookup the `Job`s of a given type.

```fsharp
// A Map from JobType to the Jobs which are of that type
let jobsForJobType =
    jobs
    |> List.groupBy (fun job -> job.JobType)
    |> Map
```

We now want to create a 1-dimensional `SliceMap` where the `'key` is a `Job` and the `'value` is the size of the job. This will make it easy for us to sum up how much work has been assigned to a `Machine`.

```fsharp
// A SliceMap where the key is a Job and the value is the size of the Job
let jobSizes =
    jobs
    |> List.map (fun job -> job, job.Size)
    |> SMap
```

Now let's create the set of `Decision`s which will represent us assigning a `Job` to a `Machine`. We will store this in a 3-dimensional `SliceMap` keyed by the `Machine`, the `JobType`, and finally the `Job`. The reason we key by the `JobType` will become apparent later in the formulation. We will use a `Boolean` decision where `1.0` indicates that we are assigning the `Job` to a `Machine` and `0.0` indicates not.

```fsharp
// The Decisions which represent assigning a Job to a Machine
// The JobType index allows us to slice along the job type
// which is useful in some of the constraints
let assignments =
    DecisionBuilder "Assignment" {
        for machine in machines do
        for jobType in machine.JobTypes do
        for job in Map.tryFindDefault jobType [] jobsForJobType ->
            Boolean
    } |> SMap3
```

Now that we have these decisions which represent assigning a job to a machine, we can formulate our first and most obvious constraints. Each job must be assigned to one machine. For each job we say that the sum of `assignments` for a given `job` across all machines and all job-types must be `1.0`. This forces the solver to find a solution where each `job` is assigned once.

```fsharp
// Each job must be assigned
let jobsAssignmentConstraints =
    ConstraintBuilder "JobAssignment" {
        for job in jobs ->
            sum assignments.[All, All, job] == 1.0
    }
```

## Constraint or Objective?

We now come to an interesting question. Our original problem statement said that we want to minimize the number of different job-types a machine must deal with. Ideally each machine only works on a single job-type. We also said that we want the machines evenly loaded. When I was chatting with my friend I dug into this point. Which one of these objectives is more important because we can't optimize for both? This is where a modeler needs to work with their client to help them understand what the most important thing is truly.

In our case, minimizing the different job-types for machines was the most important, so long as the machines were not too unevenly loaded. This means that the goal for even loading becomes a constraint and the objective remains the minimization of different job-types for machines. We will explore variations of this problem in future posts.

## Controlling the Difference in Loading

Now that we have decided that even machine loading needs to be a constraint, we need to create some `Decision`s to control for it. We will create two `Decision`s. One will represent the value of the machine with the greatest loading and the other the machine with the least loading.

```fsharp
// A Decision which is meant to represent the MaxWork value across all Machines
let maxWork = Decision.createContinuous "MaxWork" 0.0 infinity
// A Decision which is meant to represent the MinWork value across all Machines
let minWork = Decision.createContinuous "MinWork" 0.0 infinity
```

Now, we want to create a `Constraint` which states that the difference between these two values is not greater than the maximum allowed difference.

```fsharp
// We constrain the difference between the most heavily loaded machine
// and the least loaded
let maxWorkDifferenceConstraint =
    Constraint.create "MaxWorkDifferent" (maxWork - minWork <== maxWorkDifference)
```

Okay, that's great but will it do anything? Right now, there is nothing that forces the `maxWork` decision to be equal to the loading of the most heavily loaded machine. There's also nothing which forces `minWork` to be equal to the loading of the most lightly loaded machine. The solver could set the values to `0.0` and be done with it. We need to create some constraints which will force `maxWork` and `minWork` to take on the loading of the most heavily and most lightly loaded machines.

Let's create some constraints which state that the value of the `maxWork` decision must be greater than or equal to the loading of all the machines. This will force it to be a value above or equal to the maximum loading.

```fsharp
// The maxWork Decision must be greater or equal to all of the total work
// for each Machine
let maxWorkConstraints =
    ConstraintBuilder "MaxWork" {
        for machine in machines ->
            maxWork >== sum (assignments[machine, All, All] .* jobSizes)
    }
```

We do a similar thing for the `minWork` decision. In this case we will say that `minWork` must be less than or equal to all the loadings of the machines.

```fsharp
// The minWork Decision must be less or equal to all of the total work
// for each Machine
let minWorkConstraints =
    ConstraintBuilder "MinWork" {
        for machine in machines ->
            minWork <== sum (assignments[machine, All, All] .* jobSizes)
    }
```

`maxWorkConstraints` and `minWorkConstraints` will force `maxWork` and `minWork` to take on the values of the most heavily and lightly loaded machines respectively. `maxWorkDifferenceConstraint` states that the difference between `maxWork` and `minWork` must be within the permissable bounds. All together these constraints will prevent the solver from distributing jobs across machines unevenly.

## Minimizing the Job-Types for Machines

We now need to quantify how many different job-types are being assigned to machines. To do this, we will create a set of `Boolean` decisions which will indicate whether we have decided to assign a job of a given job-type to a machine. We will store these in a 2-dimensional `SliceMap` where the keys are the `Machine` and the `JobType`. `1` will represent that we have decided to assign a given `JobType` to a `Machine`. `0` will indicate that we did not. I like to think of this as "turning on" or "turning off" the job-type for the machine. We will call these decisions the `setups` decisions.

```fsharp
// A Decision which indicates whether we setup a given Machine for a 
// JobType at any point
let setups =
    DecisionBuilder "Setups" {
        for machine in machines do
        for jobType in jobTypes ->
            Boolean
    } |> SMap2
```

We now want to create some constraints which will force the solver to turn on the decision to allow the assigning of a job-type to a machine. We will do this by saying that the sum of jobs of a given job-type must be less than or equal to our decision to assign that job-type to the machine multiplied by a large number. This will force the solver to "turn on" the job-type for the machine. In our case the "large number" will be the total number of jobs.

```fsharp
// We must turn the setups value for a given Machine and JobType to 1
// if we assign a Job of the given JobType to the Machine
let setupConstraints =
    ConstraintBuilder "SetupRequired" {
        for machine in machines do
        for jobType in jobTypes ->
            sum (assignments[machine, jobType, All]) <== (float numberOfJobs) * setups[machine, jobType]
    }
```

We can now create an expression which represents the number of different job-types that are assigned to machines.

```fsharp
// An expression which is the sum of the Setups that will need to be performed
let numberSetupsExpression = sum setups
```

We use this expression to create our objective of minimizing the number of different job-types assigned to a machine.

```fsharp
// We want to minimize the number of setups
let minSetupsObjective = Objective.create "MinSetups" Minimize numberSetupsExpression
```

We can now compose our model from the parts that we have created.

```fsharp
// Compose the model
let model =
    Model.create minSetupsObjective
    |> Model.addConstraints jobsAssignmentConstraints
    |> Model.addConstraints maxWorkConstraints
    |> Model.addConstraints minWorkConstraints
    |> Model.addConstraint maxWorkDifferenceConstraint
    |> Model.addConstraints setupConstraints
```

We now ask the solver to find us a solution.

```fsharp
// Give the solver plenty of time to find a solution
let settings = { Settings.basic with MaxDuration = 60_000L }

let result = Solver.solve settings model
```

If you want to see the code that prints out the results you can check it out [here](https://github.com/matthewcrews/modelmondays/blob/260d21184df7f5803a4c6a72ba13e8b61db4962d/2021-01-22-MachineAllocation/2021-01-22-MachineAllocation.fsx#L195). This is the solution the solver found.


```terminal
Assignments:
Machine: 1
        Job: 4
        Job: 7
        Job: 8
        Job: 11
Machine: 2
        Job: 16
        Job: 18
        Job: 20
Machine: 3
        Job: 1
        Job: 3
        Job: 6
        Job: 12
        Job: 17
        Job: 19
Machine: 4
        Job: 2
        Job: 13
        Job: 14
Machine: 5
        Job: 5
        Job: 9
        Job: 10
        Job: 15

Machine Loading:
Machine: 1 | Total Load: 5
Machine: 2 | Total Load: 5
Machine: 3 | Total Load: 7
Machine: 4 | Total Load: 6
Machine: 5 | Total Load: 6

Max Diffence In Loading: 2
```

## Next Steps

We've only begone to explore this model. There are quite a few variations and nuances that I will dive into in the posts to come. In the future we will discuss adding machine capacity and dealing with infeasible models. We will also explore adding restrictions on just how much of a job-type can be assigned to a given machine. Some job-types cause more wear and therefore we do not want too much assigned to a single machine. We will also look at needing to re-plan part way through the day and look at scheduling over a longer time horizon.

These types of scheduling problems are common and therefore it's valuable for us to explore how we can play and tweak with this model to make it suit our needs. Feel free to reach out with questions and ideas for modeling challenges in the future!

Please send me an email at matthewcrews@gmail.com if you have any questions and subscribe so you can stay on top new posts and products I am offering.

{{<subscribe>}}

