TAPFacade
=========

Silverlight wrapper for using async/await TAP interfaces

Since Microsoft released the BCL.Async packages on NuGet, Silverlight has been able to enjoy the benefits of async/await to improve the readability of your code. 

...unless you wish to use it with WCF. Attempting to use SVCUTIL (or add service reference in VS) will not create APIs that use the Task-based Asynchronous Pattern (TAP), you are stuck using Silverlight's APM (Asynchronous Programming Model) to make network calls, which has a habit of fragmenting code and splitting logic across callbacks.

At my workplace, we wished to have a single TAP contract that was reusable across all platforms, and while .NET 4.5 allows you to transparently create TAP proxies, Silverlight will not help you.
