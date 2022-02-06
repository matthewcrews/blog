---
published: true
title: The Anatomy of an Optimization Model
date: 2019-11-11
---

I am on a bit of a quest to bring Mathematical Optimization to the masses, or at least to Software Developers. I often come across problems where people are wanting to find the best plan for their problem but they lack to tools to express the problem. Typically the way this is "solved" is by some domain expert coming up with a laborious heuristic in Excel which involves outrageous amount of copying and pasting. I have seen this is take place in tiny companies all the way up to multi-billion dollar enterprises. What really breaks my heart when I see this is that I know there is a better way, but people are just are not aware of it. This is why I am pushing for more training on Mathematical Optimization at my company and why I am starting a blog series on Mathematical Modeling.

## The Goal

My hope with these blog posts is to start from a barebones introduction to the concepts which undergird mathematical modeling and slowly introduce newer and more advanced modeling techniques. I don't think I will ever finish because I am always across new and interesting problems. What I will emphasize is the beautiful interplay between Machine Learning and Mathematical Optimization. I have been blessed to work on several projects where we were able to marry these tools to great effect. If you have any problems that you would like me to look at and write a blog post on, I would be happy to. My hope is to give examples which help people with their own work.

## Ingredients for an Optimization Problem

There are three things you need to have before you are reading to apply Mathematical Optimization. You need 1) A Quantifiable Objective 2) Decisions you control and 3) Rules to follow. When you have these three things, you have a problem ripe for Mathematical Optimization. Let me unpack them a little to make sure you understand what I am talking about as we move forward.

### A Quantifiable Objective

For you to apply Mathematical Optimization to a problem you need a way to measure how successful you are. Without this, a Solver will not have a way to check if the solutions that it is finding are actually an improvement or not. You are typically trying to maximize some value or minimize it. Common Objectives are Maximize Revenue, Minimize Waste, Maximize User Engagement, Minimize Energy Use, or Minimize Cost. What you are trying to achieve could really be anything, the key is that you have the ability to quantify it.

### Decisions You Control

When you create a Mathematical Model of your problem you are going to define the Decisions which need to be made. This is often something like how many people you assign to a task, how much water to put where, where to place a warehouse, which ad to show where. These are all examples of decisions you will be making in your business. The important thing is that you actually have control over them. When you solve the Optimization Model, you will get answers for what values you should use for your Decisions. It will tell you how many people to assign to a task or where to put the water or which ad to show where.

### Rules to Follow

Strictly speaking, it is possible to have an Optimization Problem without a set of rules to follow but it is rare. Most real world problems will have some kind of constraint on what you are allowed to do. Typically there are a limited number of people or only so many locations where you can place a warehouse or only so much power available. It is often the rules to follow which make a Optimization Problem interesting. Most often someone is trying to find the best plan given a set of restrictions that they need to follow. These restrictions are what make finding the best answer difficult.

### Where we go from here

Let's say that we have checked all the boxes and it looks like you have a problem which is a good candidate for Mathematical Optimization. What do we do now? We formulate a model. How we do that is what I am looking forward to opening up to you in this and future blog posts. Once we have a Model of our problem we are able to hand it to a piece of software called a Solver which knows how to take the model and search for the best possible solution.

My plan for this series is to follow a simple pattern. First, present a real world problem which will help us ground the concepts. Second, develop the mathematical model and walk through how it works. There will be some math notation but I'll walk through it slowly so you don't get lost. Thirdly, translate the model into code. I will be using Python and the PuLP library for my examples. Python is ubiquitous and the PuLP library is open source and easy to install.

In the rest of this post I will walk through a toy problem for the purpose of introducing the vocabulary of Mathematical Optimization Modeling. In future posts I will work more complex problems which will have interesting characteristics.

## The Food Truck Problem

One of the example problems I like to use is that of a Food Truck. I am from Portland, OR USA originally and we had food trucks everywhere. In this example we are running a food truck and we have to decide what items to pack for the day. We sell Hamburgers and Burritos. Hamburgers sell for \\$5.00 and Burritos sell for \\$7.50 (they are big Burritos). Each Hamburger requires us to carry 1.0 Lb. of ingredients on the truck and each Burrito requires 1.5 Lbs. (I told you they are big). We have a small Food Truck so we can only carry up to 650 Lbs. of ingredients. We also forgot to go to stock up the day before so we only have 200 Hamburger buns on hand and only 300 tortillas for Burritos. Since we run an awesome Food Truck, we always sell out of everything we bring. The question now becomes, how much Hamburgers do we pack for and how many Burritos so that we maximize our profit?

> **Note:** This example problem is meant to be simple. I am mostly concerned with introducing the vocabulary of Optimization Modeling. Future problems will be more complex.

This problem is a clear example of a Mathematical Optimization Problem. It has a Quantifiable Objective, Maximize Revenue. It has Decisions which we can control: the number of Hamburgers and Burritos we will pack for. Finally it has rules we must follow, the Food Truck can only carry 650 Lbs, we only have 200 Hamburger Buns and we only have 300 tortillas for Burritos. Now, I am going to show you how we formulate this as an Optimization Model and then I will walk through each piece so that it makes sense. For this model I am going to use the variable $x_{1}$ to represent the number of Hamburgers we are going to pack for and $x_{2}$ to represent the number of Burritos.

$$
\begin{align}
&\text{Maximize: }5.00 x_{1} + 7.50 x_{2} \\\\ 
&\text{Subject to:} \\\\
\end{align}
$$
$$
\begin{align}
x_{1} \leq& 200 \\\\
x_{2} \leq& 300 \\\\
1.0x_{1} + 1.5x_{2}\leq& 650 \\\\
x_{1}, x_{2} \geq& 0
\end{align}
$$

Let's unpack this. The first line of any Mathematical Optimization Model is going to be the Objective Function. This is the function which is used to quantify success. It will start with whether we are trying to Maximize the value of the Objective Function or Minimize it. In this case we are trying to Maximize. The formula that you see is the calculation for Revenue. Remember, $x_{1}$ is the number of Hamburgers and $x_{2}$ is the number of Burritos. For every Hamburger we will earn \\$5.00 and for each Burrito we will earn \\$7.50. This means to calculate the total revenue we multiply the number of Hamburgers by the revenue per Hamburger and the number of Burritos by the revenue per Burrito: $5.00x_{1} + 7.50x_{2}$.

After the Objective Function we get to a section referred to as the Constraints. This section typically begins with either a "Subject to" or just "S.t." as a shorthand. This section is describing the rules that we need to follow. The first constraint is our limitation on the number of Hamburgers due to the number of buns that we have. We only have 200 buns available which means that $x_{1}$ must be less than or equal to 200. We write that as a constraint in this way: $x_{2} \leq 200$. The next constraint is describing our limit on the number of Burritos we could pack since we only have 300 tortillas. $x_{2}$ represents the number of Burritos we plan to pack and it must be less than 300 therefore we add this constraint: $x_{2} \leq 300$.

The third constraint represents the weight limit of our Food Truck. We can only carry 650 Lbs. so the combination of the number of Hamburgers and the number of Burritos must be less than this. We multiply the number of Hamburgers by the lbs per Hamburger and the number of Burritos by the lbs per Burrito and add them together to get the total weight. That total must be less than the capacity of the Food Truck. This gives us this constraint: $1.0x_{1} + 1.5x_{2} \leq 650$.

The final line of the model states the number of Hamburgers and Burritos cannot be less than zero. This is implicitly obvious to us as people but a Solver won't have our context so we have to tell it that those numbers cannot be negative. This is where we are brining our added context to the problem.

## To the Code

Now that we have walked through this small problem, let's see how it translates to code. I am using Python 3.8 and PuLP 1.6.8. The first thing we do is import `PuLP` and create a new instance of a `problem` object.


```python
# Import the PuLP Library
from pulp import *

# Create an instance of a Problem object to populate
problem = LpProblem("Food Truck", LpMaximize)
```

`PuLP` provides us the tools we will need to create the Optimization Model and then solve it. Out of the box the `PuLP` library comes with some open source solvers so you can build and solve models without having to purchase a solver license.

The first argument of the `LpProblem` function is the name of our problem. The second is the type of optimization we want to perform, Maximization or Minimization. In this case we are wanting to maximize revenue so we use the argument value of `LpMaximize`. If we wanted to minimize we could have used the `LpMinimize` value.

Now let's create some decision variables to represent how many burgers and tacos we are going to bring on our food truck. We do this by using the `LpVariable` function.


```python
x1 = LpVariable("burgers", 0)
x2 = LpVariable("tacos", 0)
```

The arguments for `LpVariable` are the name of the variable and the lower bound on the possible value. In some problems, the decision variables can take on negative numbers. In this case, having negative tacos or negative hamburgers does not make any sense so we specifiy that the lower bound is 0.

We now have the Decision Variables for the problems so we can now add the Objective Function and the Constraints. Let's start with adding the Objective Function. Whenever we want to add something to a `problem` we use the `+=` operator. The `PuLP` library will infer whether we are adding an Objective Function or a Constraint based on the right hand side argument. All we need to do for the Objective Function is to provide the equation.


```python
# Add the Objective Function
problem += 5.00*x1 + 7.50*x2
```

 The `problem` object now has an Objective Function. Now let's go about adding the constraints. The first constraint is the **Max Burgers** constraint. To do this we use the `+=` operator to add constraints to our `problem` object. We then give the equation for the constraint and the name of the constraint.


```python
# Add Max Burgers constraint
problem += x1 <= 200, "Max Burgers"
```

We then need to add the **Max Burritos** constraint.


```python
# Add Max Burritos Constraint
problem += x2 <= 300, "Max Burritos"
```

Finally we need the **Max Weight** constraint.


```python
# Add Max Weight Constraint
problem += 1.0*x1 + 1.5*x2 <= 650, "Max Weight"
```

We now have a fully populated problem. To solve it, all we need to do is call the `solve()` method.


```python
# Solve the problem
problem.solve()
> 1
```


We get a numeric response back but it will not mean much until we translate it to something we can understand. Fortunately, the `PuLP` library has a dictionary which stores the mapping from the numeric status of the problem to a human readable string. This dictionary is the `LpStatus` dictionary. Let's use this to print out the string representation of the problem status.


```python
# Print the problem status
print(LpStatus[problem.status])
> Optimal
```

    

We should see the string `optimal`. This means that the solver was able to find the optimal answer. In the future we will go over the other possible statuses and what they mean.

Now, let's look at what values for the Decision Variables the Solver chose.


```python
# Loop through each of the Decision Variables in the problem
for v in problem.variables():
    # Print the name of the Variable and the Value the Solver chose
    print(v.name,'=',v.varValue)

> burgers = 200.0
> tacos = 300.0
```

Let's see what kind of Revenue we should expect if we follow this plan.


```python
# Get the expected Revenue
revenue = value(problem.objective)

# Print the expected result
print(f"${revenue:,.2f}")
> $3,250.00
```

There you have it. A tiny Mathematical Optimization problem. Granted, this was completely overkill for such a simple problem. My goal was to introduce these concepts and start growing our vocabulary around Optimization. From here we will adjust how we are managing the data and Decision Variables for our problem. This approach will not scale. In the next post we will look at adding the concept of Sets and Parameters. This will make it easy for us to create large optimization problems involving hundreds of thousands, if not millions of Decision Variables and Constraints.
