using System;

public static class FunctionExtensions
{
    #region Function Composition

    public static Func<TArg, TResult2> ComposeWith<TArg, TResult1, TResult2>(this Func<TResult1, TResult2> f1, Func<TArg, TResult1> f2)
        => arg => f1(f2(arg));

    public static Func<TResult2> ComposeWith<TResult1, TResult2>(this Func<TResult1, TResult2> f1, Func<TResult1> f2)
        => () => f1(f2());

    public static Func<TArg, TResult2> Then<TArg, TResult1, TResult2>(this Func<TArg, TResult1> f1, Func<TResult1, TResult2> f2)
        => arg => f2(f1(arg));

    public static Func<TResult2> Then<TResult1, TResult2>(this Func<TResult1> f1, Func<TResult1, TResult2> f2)
        => () => f2(f1());

    public static Func<TArg, TResult2> Compose<TArg, TResult1, TResult2>(Func<TResult1, TResult2> f1, Func<TArg, TResult1> f2)
        => arg => f1(f2(arg));

    public static Func<TArg, TResult3> Compose<TArg, TResult1, TResult2, TResult3>(Func<TResult2, TResult3> f1, Func<TResult1, TResult2> f2, Func<TArg, TResult1> f3)
        => arg => f1(f2(f3(arg)));

    public static Func<TArg, TResult4> Compose<TArg, TResult1, TResult2, TResult3, TResult4>(Func<TResult3, TResult4> f1, Func<TResult2, TResult3> f2, Func<TResult1, TResult2> f3, Func<TArg, TResult1> f4)
        => arg => f1(f2(f3(f4(arg))));

    public static Func<TArg, TResult5> Compose<TArg, TResult1, TResult2, TResult3, TResult4, TResult5>(Func<TResult4, TResult5> f1, Func<TResult3, TResult4> f2, Func<TResult2, TResult3> f3, Func<TResult1, TResult2> f4, Func<TArg, TResult1> f5)
        => arg => f1(f2(f3(f4(f5(arg)))));

    public static Func<TArg, TResult6> Compose<TArg, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6>(Func<TResult5, TResult6> f1, Func<TResult4, TResult5> f2, Func<TResult3, TResult4> f3, Func<TResult2, TResult3> f4, Func<TResult1, TResult2> f5, Func<TArg, TResult1> f6)
        => arg => f1(f2(f3(f4(f5(f6(arg))))));

    public static Func<TArg, TResult7> Compose<TArg, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7>(Func<TResult6, TResult7> f1, Func<TResult5, TResult6> f2, Func<TResult4, TResult5> f3, Func<TResult3, TResult4> f4, Func<TResult2, TResult3> f5, Func<TResult1, TResult2> f6, Func<TArg, TResult1> f7)
        => arg => f1(f2(f3(f4(f5(f6(f7(arg)))))));

    public static Func<TArg, TResult8> Compose<TArg, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8>(Func<TResult7, TResult8> f1, Func<TResult6, TResult7> f2, Func<TResult5, TResult6> f3, Func<TResult4, TResult5> f4, Func<TResult3, TResult4> f5, Func<TResult2, TResult3> f6, Func<TResult1, TResult2> f7, Func<TArg, TResult1> f8)
        => arg => f1(f2(f3(f4(f5(f6(f7(f8(arg))))))));

    public static Func<TResult2> Compose<TResult1, TResult2>(Func<TResult1, TResult2> f1, Func<TResult1> f2)
        => () => f1(f2());

    public static Func<TResult3> Compose<TResult1, TResult2, TResult3>(Func<TResult2, TResult3> f1, Func<TResult1, TResult2> f2, Func<TResult1> f3)
        => () => f1(f2(f3()));

    public static Func<TResult4> Compose<TResult1, TResult2, TResult3, TResult4>(Func<TResult3, TResult4> f1, Func<TResult2, TResult3> f2, Func<TResult1, TResult2> f3, Func<TResult1> f4)
        => () => f1(f2(f3(f4())));

    public static Func<TResult5> Compose<TResult1, TResult2, TResult3, TResult4, TResult5>(Func<TResult4, TResult5> f1, Func<TResult3, TResult4> f2, Func<TResult2, TResult3> f3, Func<TResult1, TResult2> f4, Func<TResult1> f5)
        => () => f1(f2(f3(f4(f5()))));

    public static Func<TResult6> Compose<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6>(Func<TResult5, TResult6> f1, Func<TResult4, TResult5> f2, Func<TResult3, TResult4> f3, Func<TResult2, TResult3> f4, Func<TResult1, TResult2> f5, Func<TResult1> f6)
        => () => f1(f2(f3(f4(f5(f6())))));

    public static Func<TResult7> Compose<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7>(Func<TResult6, TResult7> f1, Func<TResult5, TResult6> f2, Func<TResult4, TResult5> f3, Func<TResult3, TResult4> f4, Func<TResult2, TResult3> f5, Func<TResult1, TResult2> f6, Func<TResult1> f7)
        => () => f1(f2(f3(f4(f5(f6(f7()))))));

    public static Func<TResult8> Compose<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8>(Func<TResult7, TResult8> f1, Func<TResult6, TResult7> f2, Func<TResult5, TResult6> f3, Func<TResult4, TResult5> f4, Func<TResult3, TResult4> f5, Func<TResult2, TResult3> f6, Func<TResult1, TResult2> f7, Func<TResult1> f8)
        => () => f1(f2(f3(f4(f5(f6(f7(f8())))))));

    public static Func<TArg, TResult2> ReverseCompose<TArg, TResult1, TResult2>(Func<TArg, TResult1> f1, Func<TResult1, TResult2> f2)
        => arg => f2(f1(arg));

    public static Func<TArg, TResult3> ReverseCompose<TArg, TResult1, TResult2, TResult3>(Func<TArg, TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3)
        => arg => f3(f2(f1(arg)));

    public static Func<TArg, TResult4> ReverseCompose<TArg, TResult1, TResult2, TResult3, TResult4>(Func<TArg, TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4)
        => arg => f4(f3(f2(f1(arg))));

    public static Func<TArg, TResult5> ReverseCompose<TArg, TResult1, TResult2, TResult3, TResult4, TResult5>(Func<TArg, TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4, Func<TResult4, TResult5> f5)
        => arg => f5(f4(f3(f2(f1(arg)))));

    public static Func<TArg, TResult6> ReverseCompose<TArg, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6>(Func<TArg, TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4, Func<TResult4, TResult5> f5, Func<TResult5, TResult6> f6)
        => arg => f6(f5(f4(f3(f2(f1(arg))))));

    public static Func<TArg, TResult7> ReverseCompose<TArg, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7>(Func<TArg, TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4, Func<TResult4, TResult5> f5, Func<TResult5, TResult6> f6, Func<TResult6, TResult7> f7)
        => arg => f7(f6(f5(f4(f3(f2(f1(arg)))))));

    public static Func<TArg, TResult8> ReverseCompose<TArg, TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8>(Func<TArg, TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4, Func<TResult4, TResult5> f5, Func<TResult5, TResult6> f6, Func<TResult6, TResult7> f7, Func<TResult7, TResult8> f8)
        => arg => f8(f7(f6(f5(f4(f3(f2(f1(arg))))))));

    public static Func<TResult2> ReverseCompose<TResult1, TResult2>(Func<TResult1> f1, Func<TResult1, TResult2> f2)
        => () => f2(f1());

    public static Func<TResult3> ReverseCompose<TResult1, TResult2, TResult3>(Func<TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3)
        => () => f3(f2(f1()));

    public static Func<TResult4> ReverseCompose<TResult1, TResult2, TResult3, TResult4>(Func<TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4)
        => () => f4(f3(f2(f1())));

    public static Func<TResult5> ReverseCompose<TResult1, TResult2, TResult3, TResult4, TResult5>(Func<TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4, Func<TResult4, TResult5> f5)
        => () => f5(f4(f3(f2(f1()))));

    public static Func<TResult6> ReverseCompose<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6>(Func<TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4, Func<TResult4, TResult5> f5, Func<TResult5, TResult6> f6)
        => () => f6(f5(f4(f3(f2(f1())))));

    public static Func<TResult7> ReverseCompose<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7>(Func<TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4, Func<TResult4, TResult5> f5, Func<TResult5, TResult6> f6, Func<TResult6, TResult7> f7)
        => () => f7(f6(f5(f4(f3(f2(f1()))))));

    public static Func<TResult8> ReverseCompose<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8>(Func<TResult1> f1, Func<TResult1, TResult2> f2, Func<TResult2, TResult3> f3, Func<TResult3, TResult4> f4, Func<TResult4, TResult5> f5, Func<TResult5, TResult6> f6, Func<TResult6, TResult7> f7, Func<TResult7, TResult8> f8)
        => () => f8(f7(f6(f5(f4(f3(f2(f1())))))));

    #endregion Function Composition

    #region Currying

    public static Func<TArg1, Func<TArg2, TResult>> Curry<TArg1, TArg2, TResult>(this Func<TArg1, TArg2, TResult> f)
        => arg1 => arg2 => f(arg1, arg2);

    public static Func<TArg1, Func<TArg2, Func<TArg3, TResult>>> Curry<TArg1, TArg2, TArg3, TResult>(this Func<TArg1, TArg2, TArg3, TResult> f)
        => arg1 => arg2 => arg3 => f(arg1, arg2, arg3);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, TResult>>>> Curry<TArg1, TArg2, TArg3, TArg4, TResult>(this Func<TArg1, TArg2, TArg3, TArg4, TResult> f)
        => arg1 => arg2 => arg3 => arg4 => f(arg1, arg2, arg3, arg4);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, TResult>>>>> Curry<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> f)
        => arg1 => arg2 => arg3 => arg4 => arg5 => f(arg1, arg2, arg3, arg4, arg5);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, TResult>>>>>> Curry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> f)
        => arg1 => arg2 => arg3 => arg4 => arg5 => arg6 => f(arg1, arg2, arg3, arg4, arg5, arg6);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, Func<TArg7, TResult>>>>>>> Curry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> f)
        => arg1 => arg2 => arg3 => arg4 => arg5 => arg6 => arg7 => f(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, Func<TArg7, Func<TArg8, TResult>>>>>>>> Curry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(this Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> f)
        => arg1 => arg2 => arg3 => arg4 => arg5 => arg6 => arg7 => arg8 => f(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

    public static Func<TArg1, Action<TArg2>> Curry<TArg1, TArg2>(this Action<TArg1, TArg2> f)
        => arg1 => arg2 => f(arg1, arg2);

    public static Func<TArg1, Func<TArg2, Action<TArg3>>> Curry<TArg1, TArg2, TArg3>(this Action<TArg1, TArg2, TArg3> f)
        => arg1 => arg2 => arg3 => f(arg1, arg2, arg3);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Action<TArg4>>>> Curry<TArg1, TArg2, TArg3, TArg4>(this Action<TArg1, TArg2, TArg3, TArg4> f)
        => arg1 => arg2 => arg3 => arg4 => f(arg1, arg2, arg3, arg4);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Action<TArg5>>>>> Curry<TArg1, TArg2, TArg3, TArg4, TArg5>(this Action<TArg1, TArg2, TArg3, TArg4, TArg5> f)
        => arg1 => arg2 => arg3 => arg4 => arg5 => f(arg1, arg2, arg3, arg4, arg5);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Action<TArg6>>>>>> Curry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> f)
        => arg1 => arg2 => arg3 => arg4 => arg5 => arg6 => f(arg1, arg2, arg3, arg4, arg5, arg6);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, Action<TArg7>>>>>>> Curry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> f)
        => arg1 => arg2 => arg3 => arg4 => arg5 => arg6 => arg7 => f(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    public static Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, Func<TArg7, Action<TArg8>>>>>>>> Curry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(this Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> f)
        => arg1 => arg2 => arg3 => arg4 => arg5 => arg6 => arg7 => arg8 => f(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

    public static Func<TArg1, TArg2, TResult> Uncurry<TArg1, TArg2, TResult>(this Func<TArg1, Func<TArg2, TResult>> f)
        => (arg1, arg2) => f(arg1)(arg2);

    public static Func<TArg1, TArg2, TArg3, TResult> Uncurry<TArg1, TArg2, TArg3, TResult>(this Func<TArg1, Func<TArg2, Func<TArg3, TResult>>> f)
        => (arg1, arg2, arg3) => f(arg1)(arg2)(arg3);

    public static Func<TArg1, TArg2, TArg3, TArg4, TResult> Uncurry<TArg1, TArg2, TArg3, TArg4, TResult>(this Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, TResult>>>> f)
        => (arg1, arg2, arg3, arg4) => f(arg1)(arg2)(arg3)(arg4);

    public static Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> Uncurry<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, TResult>>>>> f)
        => (arg1, arg2, arg3, arg4, arg5) => f(arg1)(arg2)(arg3)(arg4)(arg5);

    public static Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> Uncurry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, TResult>>>>>> f)
        => (arg1, arg2, arg3, arg4, arg5, arg6) => f(arg1)(arg2)(arg3)(arg4)(arg5)(arg6);

    public static Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> Uncurry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, Func<TArg7, TResult>>>>>>> f)
        => (arg1, arg2, arg3, arg4, arg5, arg6, arg7) => f(arg1)(arg2)(arg3)(arg4)(arg5)(arg6)(arg7);

    public static Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> Uncurry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(this Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, Func<TArg7, Func<TArg8, TResult>>>>>>>> f)
        => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => f(arg1)(arg2)(arg3)(arg4)(arg5)(arg6)(arg7)(arg8);

    public static Action<TArg1, TArg2> Uncurry<TArg1, TArg2>(this Func<TArg1, Action<TArg2>> f)
        => (arg1, arg2) => f(arg1)(arg2);

    public static Action<TArg1, TArg2, TArg3> Uncurry<TArg1, TArg2, TArg3>(this Func<TArg1, Func<TArg2, Action<TArg3>>> f)
        => (arg1, arg2, arg3) => f(arg1)(arg2)(arg3);

    public static Action<TArg1, TArg2, TArg3, TArg4> Uncurry<TArg1, TArg2, TArg3, TArg4>(this Func<TArg1, Func<TArg2, Func<TArg3, Action<TArg4>>>> f)
        => (arg1, arg2, arg3, arg4) => f(arg1)(arg2)(arg3)(arg4);

    public static Action<TArg1, TArg2, TArg3, TArg4, TArg5> Uncurry<TArg1, TArg2, TArg3, TArg4, TArg5>(this Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Action<TArg5>>>>> f)
        => (arg1, arg2, arg3, arg4, arg5) => f(arg1)(arg2)(arg3)(arg4)(arg5);

    public static Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> Uncurry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Action<TArg6>>>>>> f)
        => (arg1, arg2, arg3, arg4, arg5, arg6) => f(arg1)(arg2)(arg3)(arg4)(arg5)(arg6);

    public static Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> Uncurry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, Action<TArg7>>>>>>> f)
        => (arg1, arg2, arg3, arg4, arg5, arg6, arg7) => f(arg1)(arg2)(arg3)(arg4)(arg5)(arg6)(arg7);

    public static Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> Uncurry<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(this Func<TArg1, Func<TArg2, Func<TArg3, Func<TArg4, Func<TArg5, Func<TArg6, Func<TArg7, Action<TArg8>>>>>>>> f)
        => (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => f(arg1)(arg2)(arg3)(arg4)(arg5)(arg6)(arg7)(arg8);

    #endregion Currying
}
