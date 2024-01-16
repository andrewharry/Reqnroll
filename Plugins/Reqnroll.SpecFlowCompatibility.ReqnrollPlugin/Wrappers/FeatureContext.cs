﻿using System;
using System.Globalization;
using BoDi;
using Reqnroll;

// ReSharper disable once CheckNamespace
namespace TechTalk.SpecFlow;

public class FeatureContext(Reqnroll.FeatureContext originalContext) : IFeatureContext
{
    #region Singleton
    [Obsolete("Please get the FeatureContext via Context Injection - https://go.reqnroll.net/Migrate-FeatureContext-Current")]
    public static Reqnroll.FeatureContext Current => Reqnroll.FeatureContext.Current;
    #endregion

    public Exception TestError => originalContext.TestError;

    public FeatureInfo FeatureInfo => originalContext.FeatureInfo;

    public CultureInfo BindingCulture => originalContext.BindingCulture;

    public IObjectContainer FeatureContainer => originalContext.FeatureContainer;
}
