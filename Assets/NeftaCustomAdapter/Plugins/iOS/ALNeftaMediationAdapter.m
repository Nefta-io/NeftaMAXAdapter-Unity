//
//  ALNeftaMediationAdapter.m
//  NeftaMaxAdapter
//
//  Created by Tomaz Treven on 09/11/2023.
//

#import "ALNeftaMediationAdapter.h"
#import "ALNeftaBanner.h"
#import "ALNeftaInterstitial.h"
#import "ALNeftaRewarded.h"

@implementation ALNeftaMediationAdapter

static NeftaPlugin *_plugin;

- (void)initializeWithParameters:(id<MAAdapterInitializationParameters>)parameters completionHandler:(void (^)(MAAdapterInitializationStatus, NSString *_Nullable))completionHandler {
    if (_plugin != nil) {
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    } else {
        NSString *appId = parameters.serverParameters[@"app_id"];
        
        _plugin = [NeftaPlugin InitWithAppId: appId];
        [_plugin EnableAds: true];
        
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    }
}

- (NSString *)SDKVersion {
    return NeftaPlugin.Version;
}

- (NSString *)adapterVersion {
    return @"2.0.0";
}

- (void)destroy {
    [_ad Close];
}

- (void)loadAdViewAdForParameters:(id<MAAdapterResponseParameters>)parameters adFormat:(MAAdFormat *)adFormat andNotify:(id<MAAdViewAdapterDelegate>)delegate {
    _ad = [[ALNeftaBanner alloc] initWithId: parameters.thirdPartyAdPlacementIdentifier listener: delegate];
    [self Load: parameters];
}

- (void)loadInterstitialAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MAInterstitialAdapterDelegate>)delegate {
    _ad = [[ALNeftaInterstitial alloc] initWithId: parameters.thirdPartyAdPlacementIdentifier listener: delegate];
    [self Load: parameters];
}

- (void)showInterstitialAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MAInterstitialAdapterDelegate>)delegate {
    int readyStatus = [_ad CanShow];
    if (readyStatus == NAd.Loading) {
        [delegate didFailToDisplayInterstitialAdWithError: MAAdapterError.adNotReady];
        return;
    }
    if (readyStatus == NAd.Expired) {
        [delegate didFailToDisplayInterstitialAdWithError: MAAdapterError.adExpiredError];
        return;
    }
    if (readyStatus != NAd.Ready) {
        [delegate didFailToDisplayInterstitialAdWithError: MAAdapterError.unspecified];
        return;
    }
    
    [_ad Show];
}

- (void)loadRewardedAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MARewardedAdapterDelegate>)delegate {
    _ad = [[ALNeftaRewarded alloc] initWithId: parameters.thirdPartyAdPlacementIdentifier listener: delegate];
    [self Load: parameters];
}

- (void)showRewardedAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MARewardedAdapterDelegate>)delegate {
    int readyStatus = [_ad CanShow];
    if (readyStatus == NAd.Loading) {
        [delegate didFailToLoadRewardedAdWithError: MAAdapterError.adNotReady];
        return;
    }
    if (readyStatus == NAd.Expired) {
        [delegate didFailToLoadRewardedAdWithError: MAAdapterError.adExpiredError];
        return;
    }
    if (readyStatus != NAd.Ready) {
        [delegate didFailToLoadRewardedAdWithError: MAAdapterError.unspecified];
        return;
    }

    [_ad Show];
}

- (void)Load:(id<MAAdapterResponseParameters>)parameters {
    UIViewController *viewController;
    if (ALSdk.versionCode >= 11020199) {
        viewController = parameters.presentingViewController ?: [ALUtils topViewControllerFromKeyWindow];
    } else {
        viewController = [ALUtils topViewControllerFromKeyWindow];
    }
    [_plugin PrepareRendererWithViewController: viewController];
    
    if (parameters.customParameters != nil && [parameters.customParameters count] > 0) {
        NSError *error;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:parameters.customParameters options:0 error:&error];
        if (!jsonData) {
            NSLog(@"Error converting dictionary to JSON: %@", error.localizedDescription);
        } else {
            NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
            [_ad SetCustomParameterWithProvider: @"applovin-max" value: jsonString];
        }
    }
    
    [_ad Load];
}
@end
