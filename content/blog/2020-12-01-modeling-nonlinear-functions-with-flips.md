---
title: Modeling Non-Linear Functions with Flips
date: 2020-12-01
draft: true
---

Recently I was asked if it would be possible to add the `log` function to the [Flips library](https://flipslibrary.com). Flips is a library for modeling and solving Linear and Mixed-Integer Programming problems. Both of these classes of problems are constrained to only having linear (i.e. straight) lines. You may ask, "What do you mean by straight?" The following are examples of linear functions.

$$
\displaylines{
\text{Linear Functions}\\\\
y=1.0x+2.0 \\\\
y=2.0x_{1}+3.0x_{2} \\\\
y=1.2x_{1}+1.7x_{2}+x_{3}
}
$$

The following are non-linear functions.

$$
\displaylines{
\text{Non-Linear Functions} \\\\
y=1.0x^2+2.0 \\\\
y=2.0/x_{1}+3.0x_{2} \\\\
y=1.2x_{1}+1.7x_{2}\times x_{3}
}
$$

For a function to be linear in the domain of Linear/Mixed-Integer Programming the variables can only be added, subtracted, or multiplied by a coefficient. The reason this is important is because a Solver takes advantage of this structure which searching for solutions.

## What if we need a Non-Linear Function

Fortunately we have ways of working around this limitation. Another way to think of a curve is just a series of straight lines. We could approximate our curve using a series of straight lines that were close enough to the original function to make our answer meaningful. For this example let's try modeling the parabola $y=-x^2+10.0$. We will use this to represent the Objective Function of our model. Below you see a plot which has a smooth grey line for the exact values of our parabola and a series of point connect by blue line segments. You will notice that the blue line segments closely match the shape of the parabola.

![Smooth Function as Line Segments](/img/2020-12-01-line-segments.png)

Our goal is to now model our original parabola with a series of segments. We will create a Decision variable which corresponds to each point on the plot. To get a value along the line segments we take a percent of the adjacent points. If I wanted the value of $y$ at the point $x=0.5$, I would use 50% of the value of $x$ at 0.0 and 50% of the value of $x$ at $1.0$. You may recognize this as [linear-interpolation](https://en.wikipedia.org/wiki/Linear_interpolation). If we want a value for x that is between our Decision variable, we just use a percent of the adjacent decisions. Let's get to the code!

We open up the `Flips` library and generate the set of points we want Decisions for. We create a range of values from `-5.0` to `5.0` and provide an index for the value. We extract the index values to be elswhere in our code.

```fsharp
open Flips
open Flips.Types
open Flips.SliceMap

// The Range of values we want to consider and the index for the value
let valueRange =
    [-5.0..5.0]
    |> List.mapi (fun index value -> index, value)

// We will need the indices for the vertices of our lines
let indices = valueRange |> List.map fst
```

We now want to create a `Decision` which corresponds for each of these points.

```
// Create a decision variable for each point
let decs =
    DecisionBuilder "Amount" {
        for i in indices ->
            Continuous (0.0, 1.0)
    } |> SMap
```

Next we need to create a constraints which says the total percentage of the points that we use must be equal to `1.0`. This ensures that the solver is selecting a point along one of our segments.

```fsharp
// We create a constraint saying that we must 
let totalOneConstraint = Constraint.create "TotalValue" (sum decs == 1.0)
```

One of the other rules that we need to impose is that the Solver can only use adjacent points for interpolation. It would make no sense if the Solver interpolated between the points `-5.0` and `5.0`. To enforce this behavior we are going to need to create an additional set of Decisions which correspond to the adjacent points along our line. We use the `List.pairwise` function to iterate through the adjacent indices and create the corresponding `Decision`. This decision type will be a `Boolean` because we either want the solver to use the pair of points or to not use them at all.

```fsharp
// We create an indicator variable which corresponds to pairs of points
// on the line we are modeling
let usePairDecisions =
    DecisionBuilder "UsePair" {
        for pair in List.pairwise indices ->
            Boolean
    } |> SMap
```

Now that we have a `Boolean` decision which corresponds to the pairs of Decisions, we need to create a set of constraints which will ensure that the Solver is only using one pair of points. We will do this with two types of constraints. The first constraint states that only one of the Pair decisions can be on at any given time.

```fsharp
// A constraint stating that only one pair may be used
let onlyOnePair = Constraint.create "OnlyOnePair" (sum usePairDecisions == 1.0)
```

The second type of constraints is for each pair of points. It states that if the `usePairDecision` is set to `1.0`, then the Solver must assign a total of `1.0` to the two corresponding decisions.

```fsharp
// We state that if we want to use the pair of vertices,
// the indicator variable associated with that pair must
// be on as well
let pairConstraints =
    ConstraintBuilder "UsePair" {
        for KeyValue ((i, j), d) in usePairDecisions ->
            decs.[i] + decs.[j] >== d
    }
```

We now have all the structure we need in place to solve a model using our approximation of the parabola. We create a `costExpression` which gives us the simplication of the parabola.

```fsharp
// We create a expression that is an approximation of our parabola
let costExpression =
    List.sum [for (i, v) in valueRange -> (-1.0 * v ** 2.0 + 10.0) * decs.[i] ]
```

We take the `costExpression` and use that to create our `Objective`. From there we create the `model` and populate it with the constraints we created earlier.

```fsharp
// We creat an objective which is to maximize our expression
let objective = Objective.create "MaxValue" Maximize costExpression

// We create a model and add the constraints
let model =
    Model.create objective
    |> Model.addConstraints pairConstraints
    |> Model.addConstraint onlyOnePair
    |> Model.addConstraint totalOneConstraint
```

We are now ready to solve the model. We are only using the basic settings since this is such a simple problem. We call solve and print out the results.

```fsharp
// We are only using basic settings
let settings =  Settings.basic

// We attempt to solve the problem and return the result
let result = Solver.solve settings model

// If the result is a success, we print out the value of the expression
// we were maximizing
match result with
| Optimal solution ->
    printfn "Objective Value: %f" (Objective.evaluate solution objective)
```

The result that is printed out...

```console
Objective Value: 10.000000
val it : unit = ()
```

We can validate this result visually by looking at the plot above.

## Constraints on Non-Linear Functions

To make things more interesting, let's add a constraint which says that our parabola can only go up to `-1.0`. This would correspond to saying $x\leq -1.0$. Now remember, we don't actually have a single $x$, we have a series of them which correspond to the different points on our plat. So how do we model this? Quite easily! We add a constraint which says the value of our decisions multiplied the corresponding `y` value, must be less or equal to `-1.0`.

```fsharp
let lessThanNegativeOne = 
    let valueExpression = List.sum [for (idx, v) in valueRange -> v * decs.[idx]]
    Constraint.create "LessThan-1.0" (valueExpression <== -1.0)
```

We can use the same code for creating the model and solving. We just add our new constraint to the model.

```fsharp
let model =
    Model.create objective
    |> Model.addConstraints pairConstraints
    |> Model.addConstraint onlyOnePair
    |> Model.addConstraint decisionsTotalToOne
    |> Model.addConstraint lessThanNegativeOne
    
let settings =  Settings.basic

let result = Solver.solve settings model

match result with
| Optimal solution ->
    printfn "Objective Value: %f" (Objective.evaluate solution objective)
```

And the result...

```console
Objective Value: 9.000000
val it : unit = ()
```

Again, we look at our plot and this makes sense. Hopefully that provides a little insight into how to model non-linear functions using linear approximations.