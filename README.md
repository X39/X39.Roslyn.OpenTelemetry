<!-- TOC -->
* [X39.Roslyn.OpenTelemetry](#x39roslynopentelemetry)
  * [Overview](#overview)
  * [Semantic Versioning](#semantic-versioning)
* [Quick Start](#quick-start)
  * [Attributes](#attributes)
    * [`ActivityAttribute`](#activityattribute)
      * [`Name` Property](#name-property)
      * [`IsRoot` Property](#isroot-property)
      * [`CreateActivitySource` Property](#createactivitysource-property)
    * [`ActivitySourceReferenceAttribute`](#activitysourcereferenceattribute)
* [How things work](#how-things-work)
  * [`ActivitySource` detection rules](#activitysource-detection-rules)
    * [Auto-Detection of ActivitySource fields](#auto-detection-of-activitysource-fields)
      * [Example](#example)
    * [`ActivitySourceReference` on assembly, class or method level](#activitysourcereference-on-assembly-class-or-method-level)
      * [Example](#example-1)
    * [`ActivityAttribtue.CreateActivitySource` being true](#activityattribtuecreateactivitysource-being-true)
      * [Example](#example-2)
  * [Custom `ActivityContext`](#custom-activitycontext)
  * [Activity name detection rules](#activity-name-detection-rules)
  * [The method prototype](#the-method-prototype)
  * [Adding tags to an `Activity`](#adding-tags-to-an-activity)
  * [Passing in `ActivityLink`s to other activities](#passing-in-activitylinks-to-other-activities)
* [About](#about)
  * [Building](#building)
  * [Testing](#testing)
  * [Contributing](#contributing)
    * [Code of Conduct](#code-of-conduct)
    * [Contributors Agreement](#contributors-agreement)
  * [License](#license)
<!-- TOC -->

<!-- This is heavily AI written as i was too lazy to be bothered with explaining everything .. ToDo: create better readme -->

# X39.Roslyn.OpenTelemetry

## Overview

**X39.Roslyn.OpenTelemetry** is a .NET library that provides a simplified
way to integrate OpenTelemetry tracing into your projects by utilizing
Roslyn Source Generators.
The project is designed to simplify the creation and management of
`System.Diagnostics.Activity` objects, which are fundamental to distributed tracing.

## Semantic Versioning

This library follows the principles of [Semantic Versioning](https://semver.org/). This means that version numbers and
the way they change convey meaning about the underlying changes in the library. For example, if a minor version number
changes (e.g., 1.1 to 1.2), this indicates that new features have been added in a backwards-compatible manner.

# Quick Start

First, install this library by running `dotnet add package X39.Roslyn.OpenTelemetry`.
After that, start adding the newly introduced attributes onto partial methods as presented in the
following example:

```csharp
// MyActivitySources.cs
public static class MyActivitySources
{
    public static readonly ActivitySource MyActivitySource = new ("MySource");
}

// MyClass.cs
[ActivitySourceReference("MyActivitySources.MyActivitySource")]
public partial class MyClass
{
    [InternalActivity]
    public static partial StartSampleActivity(DateTime timeStamp, string note);
    
    public void SampleMethod()
    {
        using var activity = StartSampleActivity(DateTime.Now, "theese will be tags in the activity span");
        // ...
    }
}
```

Do note that you may also put the `ActivitySourceReference` on the partial method itself to override behavior
or ditch it entirely if you pass in your `ActivitySource` via DI and store it in some variable or property.

## Attributes

### `ActivityAttribute`

The `ActivityAttribute` is the core attribute of this library.
It requires you to provide an `ActivityKind` in the constructor and offers additional
named properties for additional details.

Do note that this attribute also has derivatives, which allow you to express the
`ActivityKind` in the attribute name (recommended to use those) to reduce verbosity when applied:

- `InternalActivityAttribute`
- `ServerActivityAttribute`
- `ClientActivityAttribute`
- `ProducerActivityAttribute`
- `ConsumerActivityAttribute`

#### `Name` Property

The name property allows you to override the default name detection rules.
See [Activity name detection rules](#activity-name-detection-rules) for more information.

#### `IsRoot` Property

By default, the activity framework in dotnet is "parent aware", automatically taking over any
parent trace and span id into "child" activities. This is not always desired and requires boilerplate
code to fix.
When setting this property to `true`, a new trace id is generated. It is recommended to provide an
`ActivityLink` to keep the trace route tho.
See [Passing in `ActivityLink`s to other activities](#passing-in-activitylinks-to-other-activities)
for more information.

#### `CreateActivitySource` Property

In certain cases, an activity may need to generate an associated `ActivitySource`.
To achieve this, you can use the `CreateActivitySource` method instead of the
`ActivitySourceReferenceAttribute`, directing the source generator to
create the corresponding `ActivitySource`.

The generated `ActivitySource` will share the same name as the activity,
meaning the value of the [`Name` Property](#name-property) will also affect
the name of the generated `ActivitySource`.

### `ActivitySourceReferenceAttribute`

The `ActivitySourceReferenceAttribute` can be used on either `class` or method level to
set the (C#) code path to the `ActivitySource`. The closer attribute will win in the resolution.

It is mandatory in all cases where the `ActivitySource` is not available as either property or field.

# How things work

## `ActivitySource` detection rules

The source generator has three ways to define the `ActivitySource` to be used with an activity

### Auto-Detection of ActivitySource fields

<!-- ToDo: Add diagnostic to handle multiple ActivitySources being found -->
Albeit being listed first, this has the lowest priority. The source generator will attempt to find
any property or field with the type `ActivitySource` in a `class` and use the first one it finds.

Do note that if the field or property found is non-static, the decorated method also must be non-static!

#### Example

```csharp
public partial class MyClass
{
    private ActivitySource _activitySource;
    public MyClass(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }
    
    [InternalActivity]
    private partial StartMyActivity();
}
```

### `ActivitySourceReference` on assembly, class or method level

When an `ActivitySourceReference` attribute is specified,
the source generator uses the provided code snippet from the string to obtain the`ActivitySource`.
The priority is determined by the proximity to the method (method > class > assembly).

Please note that no validation is performed as this code is considered user-defined.
You can include whatever logic is required to retrieve the `ActivitySource`,
as long as it fits within a single expression.

#### Example

```csharp
[ActivitySourceReference("Statics.ApplicationActivitySource")]
public partial class MyClass
{
    [InternalActivity]
    private partial StartMyActivity();
}
```

### `ActivityAttribtue.CreateActivitySource` being true

If you instruct the source generator to create an `ActivitySource` for a method, that one always will
take precedence over any other rules.

#### Example

```csharp
public partial class MyClass
{
    [InternalActivity(CreateActivitySource = true)]
    private partial StartMyActivity();
}
```

## Custom `ActivityContext`

You may want to pass in a specific activity context into an activity.
To do that, you may simply add a single `ActivitySource` parameter to your method (position does not matter).
Note that the default `ActivityContext` will always keep the activity chain.

<!-- ToDo: Add diagnostic to check no IsRoot and ActivityContext parameter -->
This parameter is incompatible with the [`IsRoot` Property](#isroot-property).
If both are provided, the `IsRoot` variant always will take precedence.

## Activity name detection rules

By default, the activity name will be extracted from the method name automagically.
E.g.:

| Method name     | Activity name |
|-----------------|---------------|
| StartMyActivity | My            |
| MoreActivity    | More          |
| StartFoobar     | Foobar        |

You may use the [`Name` Property](#name-property) to overrule the detection if desired.

## The method prototype

The method always must return `Activity?` and be partial.
Other than that, you may formulate the method to your liking.

Note that if your `ActivitySource` is provided by a non-static field, the method must be non-static too.

E.g.:

- `private partial Activity? StartMyActivity()`
- `private partial Activity? StartMyActivity(string tag1)`
- `public static partial Activity? StartMyActivity(object someObj)`
- `internal partial Activity? StartMyActivity(int i, double j, bool flag)`

## Adding tags to an `Activity`

Tags are vital to enrich your activities. While you may use the readily available methods to add tags
to a started activity as per usual, adding them at creation sometimes is mandatory.
To archive this, simply add a parameter with the tag you want.
The source generator will pick that parameter up and puts it into the corresponding
`KeyValuePair<string, object?>[]` passed into `StartActivity`.

## Passing in `ActivityLink`s to other activities

To add an `ActivityLink` to an activity, you just have, and probably already guessed by now, to add
a corresponding parameter. The source generator will take your `ActivityLink` and throw it at the correct
place too.

# About

## Building

This project uses GitHub Actions for continuous integration.
The workflow is defined in `.github/workflows/main.yml`.
It includes steps for restoring dependencies, building the project, and publishing a NuGet package.

## Testing

The source generator utilizes automated tests, automatically ran on every merge or push on master
and will prevent publishing if any fails.

This guarantees that behavior is always as expected when publishing to NuGet.

## Contributing

Contributions are welcome!
Please submit a pull request or create a discussion to discuss any changes you wish to make.

**Do check the [Contributors Agreement](#contributors-agreement) first tho!**

### Code of Conduct

Be excellent to each other.

### Contributors Agreement

First of all, thank you for your interest in contributing to this project!
Please add yourself to the list of contributors in the [CONTRIBUTORS](CONTRIBUTORS.md) file when submitting your
first pull request.
Also, please always add the following to your pull request:

```
By contributing to this project, you agree to the following terms:
- You grant me and any other person who receives a copy of this project the right to use your contribution under the
  terms of the GNU Lesser General Public License v3.0.
- You grant me and any other person who receives a copy of this project the right to relicense your contribution under
  any other license.
- You grant me and any other person who receives a copy of this project the right to change your contribution.
- You waive your right to your contribution and transfer all rights to me and every user of this project.
- You agree that your contribution is free of any third-party rights.
- You agree that your contribution is given without any compensation.
- You agree that I may remove your contribution at any time for any reason.
- You confirm that you have the right to grant the above rights and that you are not violating any third-party rights
  by granting these rights.
- You confirm that your contribution is not subject to any license agreement or other agreement or obligation, which
  conflicts with the above terms.
```

This is necessary to ensure that this project can be licensed under the GNU Lesser General Public License v3.0 and
that a license change is possible in the future if necessary (e.g., to a more permissive license).
It also ensures that I can remove your contribution if necessary (e.g., because it violates third-party rights) and
that I can change your contribution if necessary (e.g., to fix a typo, change implementation details, or improve
performance).
It also shields me and every user of this project from any liability regarding your contribution by deflecting any
potential liability caused by your contribution to you (e.g., if your contribution violates the rights of your
employer).
Feel free to discuss this agreement in the discussions section of this repository, i am open to changes here (as long as
they do not open me or any other user of this project to any liability due to a **malicious contribution**).

## License

This project is licensed under the GNU Lesser General Public License v3.0.
See the [LICENSE](LICENSE) file for details.

Note: As many do not understand how GPL licenses work, the brief summary is:
> You may use this library in commercial projects without changing your project,
> as long as you **dynamically** link the library, without changing your license to LGPL.

Contact me if you have further questions.