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

NSString * const _mediationProvider = @"applovin-max";

@implementation ALNeftaMediationAdapter

static NeftaPlugin *_plugin;

+(void) OnExternalAdShown:(MAAd*)ad {
    NSMutableDictionary *data = [NSMutableDictionary dictionary];
    [data setObject: _mediationProvider forKey: @"mediation_provider"];
    [data setObject: ad.format.label forKey: @"format"];
    [data setObject: [NSString stringWithFormat:@"%dx%d", (int)ad.size.width, (int)ad.size.height] forKey: @"size"];
    [data setObject: ad.adUnitIdentifier forKey: @"adUnit_id"];
    [data setObject: ad.networkName forKey: @"network_name"];
    if (ad.creativeIdentifier != nil) {
        [data setObject: ad.creativeIdentifier forKey: @"creative_id"];
    }
    [data setObject: @(ad.revenue) forKey: @"revenue"];
    [data setObject: ad.revenuePrecision forKey: @"revenue_precision"];
    if (ad.placement != nil) {
        [data setObject: ad.placement forKey: @"placement_name"];
    }
    [data setObject: @((NSInteger)(ad.requestLatency * 1000)) forKey: @"request_latency"];
    if (ad.DSPName != nil) {
        [data setObject: ad.DSPName forKey: @"dsp_name"];
    }
    if (ad.DSPIdentifier != nil) {
        [data setObject: ad.DSPIdentifier forKey: @"dsp_id"];
    }
    MAAdWaterfallInfo* waterfall = ad.waterfall;
    if (waterfall != nil) {
        if (waterfall.name != nil) {
            [data setObject: waterfall.name forKey: @"waterfall_name"];
        }
        if (waterfall.testName != nil) {
            [data setObject: waterfall.testName forKey: @"waterfall_test_name"];
        }
        NSMutableArray *waterfalls = [NSMutableArray array];
        for (MANetworkResponseInfo *other in waterfall.networkResponses) {
            NSString *name = other.mediatedNetwork.name;
            if (name == nil || [name length] == 0) {
                id n = other.credentials[@"network_name"];
                if ([n isKindOfClass:[NSString class]]) {
                    name = (NSString *)n;
                }
            }
            if (name != nil && [name length] > 0) {
                [waterfalls addObject: name];
            }
        }
        [data setObject: waterfalls forKey: @"waterfall"];
    }
    [NeftaPlugin OnExternalAdShown: @"max" data: data];
}

- (void)initializeWithParameters:(id<MAAdapterInitializationParameters>)parameters completionHandler:(void (^)(MAAdapterInitializationStatus, NSString *_Nullable))completionHandler {
    if (_plugin != nil) {
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    } else {
        NSString *appId = parameters.serverParameters[@"app_id"];
        
        _plugin = [NeftaPlugin InitWithAppId: appId];
        NSNumber *hasConsent = [parameters hasUserConsent];
        NSNumber *isDoNotSell = [parameters isDoNotSell];
        [_plugin SetTrackingWithIsAuthorized: hasConsent != nil && hasConsent.longLongValue == 1 && (isDoNotSell == nil || isDoNotSell.longLongValue == 0)];
        
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    }
}

- (NSString *)SDKVersion {
    return NeftaPlugin.Version;
}

- (NSString *)adapterVersion {
    return @"2.1.0";
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

    ALNeftaRewarded *rewarded = (ALNeftaRewarded *) _ad;
    rewarded.reward = [self reward];
    rewarded.giveReward = [self shouldAlwaysRewardUser];
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
            [_ad SetCustomParameterWithProvider: _mediationProvider value: jsonString];
        }
    }
    
    [_ad Load];
}
@end
