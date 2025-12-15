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

#import <os/log.h>
#import <AppLovinSDK/AppLovinSDK.h>

NSString * const _mediationProvider = @"applovin-max";

@implementation ALNeftaMediationAdapter

static NeftaPlugin *_plugin;

+ (void)OnExternalMediationRequestWithBanner:(MAAdView * _Nonnull)banner insight:(AdInsight * _Nullable)insight {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[banner hash]];
    [ALNeftaMediationAdapter OnExternalMediationRequest: AdTypeBanner id: hash requestedAdUnitId: banner.adUnitIdentifier insight: insight];
}
+ (void)OnExternalMediationRequestWithBanner:(MAAdView * _Nonnull)banner {
    [ALNeftaMediationAdapter OnExternalMediationRequestWithBanner: banner customBidPrice: -1];
}
+ (void)OnExternalMediationRequestWithBanner:(MAAdView * _Nonnull)banner customBidPrice:(double)customBidPrice {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[banner hash]];
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: AdTypeBanner id: hash requestedAdUnitId: banner.adUnitIdentifier requestedFloorPrice: customBidPrice requestId: -1];
}

+ (void)OnExternalMediationRequestWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial insight:(AdInsight * _Nullable)insight {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[interstitial hash]];
    [ALNeftaMediationAdapter OnExternalMediationRequest: AdTypeInterstitial id: hash requestedAdUnitId: interstitial.adUnitIdentifier insight: insight];
}
+ (void)OnExternalMediationRequestWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial {
    [ALNeftaMediationAdapter OnExternalMediationRequestWithInterstitial: interstitial customBidPrice: -1];
}
+ (void)OnExternalMediationRequestWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial customBidPrice:(double)customBidPrice {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[interstitial hash]];
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: AdTypeInterstitial id: hash requestedAdUnitId: interstitial.adUnitIdentifier requestedFloorPrice: customBidPrice requestId: -1];
}

+ (void)OnExternalMediationRequestWithRewarded:(MARewardedAd * _Nonnull)rewarded insight:(AdInsight * _Nullable)insight {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[rewarded hash]];
    [ALNeftaMediationAdapter OnExternalMediationRequest: AdTypeRewarded id: hash requestedAdUnitId: rewarded.adUnitIdentifier insight: insight];
}
+ (void)OnExternalMediationRequestWithRewarded:(MARewardedAd * _Nonnull)rewarded {
    [ALNeftaMediationAdapter OnExternalMediationRequestWithRewarded: rewarded customBidPrice: -1];
}
+ (void)OnExternalMediationRequestWithRewarded:(MARewardedAd * _Nonnull)rewarded customBidPrice:(double)customBidPrice {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[rewarded hash]];
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: AdTypeRewarded id: hash requestedAdUnitId: rewarded.adUnitIdentifier requestedFloorPrice: customBidPrice requestId: -1];
}

+ (void)OnExternalMediationRequest:(AdType)adType id:(NSString * _Nonnull)id requestedAdUnitId:(NSString * _Nonnull)requestedAdUnitId insight:(AdInsight * _Nullable)insight {
    int requestId = -1;
    double requestedFloor = -1;
    if (insight != nil) {
        requestId = (int)insight._requestId;
        requestedFloor = insight._floorPrice;
    }
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: (int)adType id: id requestedAdUnitId: requestedAdUnitId requestedFloorPrice: requestedFloor requestId: requestId];
}

+ (void)OnExternalMediationRequestLoadWithBanner:(MAAdView * _Nonnull)banner ad:(MAAd * _Nonnull)ad {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[banner hash]];
    [ALNeftaMediationAdapter OnExternalMediationResponseLoad: hash ad: ad];
}
+ (void)OnExternalMediationRequestLoadWithInterstitial:(MAAdView * _Nonnull)interstitial ad:(MAAd * _Nonnull)ad {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[interstitial hash]];
    [ALNeftaMediationAdapter OnExternalMediationResponseLoad: hash ad: ad];
}
+ (void)OnExternalMediationRequestLoadWithRewarded:(MAAdView * _Nonnull)rewarded ad:(MAAd * _Nonnull)ad {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[rewarded hash]];
    [ALNeftaMediationAdapter OnExternalMediationResponseLoad: hash ad: ad];
}
+ (void)OnExternalMediationResponseLoad:(NSString * _Nonnull)id ad:(MAAd * _Nonnull)ad {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[ad hash]];
    NSMutableDictionary *baseObject = nil;
    MAAdWaterfallInfo *waterfall = ad.waterfall;
    if (waterfall != nil) {
        baseObject = [NSMutableDictionary dictionary];
        [ALNeftaMediationAdapter SerializeWaterfall: baseObject waterfall:waterfall];
    }
    [NeftaPlugin OnExternalMediationResponse: _mediationProvider id: id id2: hash revenue: ad.revenue precision: ad.revenuePrecision status: 1 providerStatus: nil networkStatus: nil baseObject: baseObject];
}

+ (void)OnExternalMediationRequestFailWithBanner:(MAAdView * _Nonnull)banner error:(MAError * _Nonnull)error {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[banner hash]];
    [ALNeftaMediationAdapter OnExternalMediationResponseFail: hash error: error];
}
+ (void)OnExternalMediationRequestFailWithInterstitial:(MAAdView * _Nonnull)interstitial error:(MAError * _Nonnull)error {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[interstitial hash]];
    [ALNeftaMediationAdapter OnExternalMediationResponseFail: hash error: error];
}
+ (void)OnExternalMediationRequestFailWithRewarded:(MAAdView * _Nonnull)rewarded error:(MAError * _Nonnull)error {
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[rewarded hash]];
    [ALNeftaMediationAdapter OnExternalMediationResponseFail: hash error: error];
}
+ (void)OnExternalMediationResponseFail:(NSString * _Nonnull)id error:(MAError * _Nonnull)error {
    int status = 0;
    if (error.code == MAErrorCodeNoFill) {
        status = 2;
    }
    NSMutableDictionary *baseObject = nil;
    MAAdWaterfallInfo *waterfall = error.waterfall;
    if (waterfall != nil) {
        baseObject = [NSMutableDictionary dictionary];
        [ALNeftaMediationAdapter SerializeWaterfall: baseObject waterfall:waterfall];
    }
    NSString *providerStatus = [NSString stringWithFormat:@"%ld", error.code];
    NSString *networkStatus = [NSString stringWithFormat:@"%ld", error.mediatedNetworkErrorCode];
    [NeftaPlugin OnExternalMediationResponse: _mediationProvider id: id id2: nil revenue: -1 precision: nil status: status providerStatus: providerStatus networkStatus: networkStatus baseObject: baseObject];
}

+ (void) OnExternalMediationImpression:(MAAd*)ad {
    [ALNeftaMediationAdapter OnExternalMediationImpression: false ad: ad];
}

+ (void) OnExternalMediationClick:(MAAd*)ad {
    [ALNeftaMediationAdapter OnExternalMediationImpression: true ad: ad];
}

+ (void) OnExternalMediationImpression:(BOOL)isClick ad:(MAAd*)ad {
    NSMutableDictionary *data = [NSMutableDictionary dictionary];
    [data setObject: ad.format.label forKey: @"format"];
    [data setObject: [NSString stringWithFormat:@"%dx%d", (int)ad.size.width, (int)ad.size.height] forKey: @"size"];
    [data setObject: ad.adUnitIdentifier forKey: @"ad_unit_id"];
    [data setObject: ad.networkName forKey: @"network_name"];
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
    if (ad.creativeIdentifier != nil) {
        [data setObject: ad.creativeIdentifier forKey: @"creative_id"];
    }
    MAAdWaterfallInfo *waterfall = ad.waterfall;
    if (waterfall != nil) {
        [ALNeftaMediationAdapter SerializeWaterfall: data waterfall: waterfall];
    }
    int type = 0;
    if (ad.format == MAAdFormat.banner || ad.format == MAAdFormat.leader || ad.format == MAAdFormat.mrec) {
        type = 1;
    } else if (ad.format == MAAdFormat.interstitial) {
        type = 2;
    } else if (ad.format == MAAdFormat.rewarded) {
        type = 3;
    }
    NSString *hash = [NSString stringWithFormat:@"%lu", (unsigned long)[ad hash]];
    [NeftaPlugin OnExternalMediationImpression: isClick provider: _mediationProvider data: data id: nil id2: hash];
}

+ (void)SerializeWaterfall:(NSMutableDictionary * _Nonnull)data waterfall:(MAAdWaterfallInfo * _Nonnull)waterfall {
    if (waterfall.name != nil) {
        [data setObject: waterfall.name forKey: @"waterfall_name"];
    }
    if (waterfall.testName != nil) {
        [data setObject: waterfall.testName forKey: @"waterfall_test_name"];
    }
    NSMutableArray *waterfalls = [NSMutableArray array];
    NSMutableArray *waterfallResponses = [NSMutableArray array];
    for (MANetworkResponseInfo *other in waterfall.networkResponses) {
        NSString *name = other.mediatedNetwork.name;
        if (name == nil || [name length] == 0) {
            id n = other.credentials[@"network_name"];
            if ([n isKindOfClass:[NSString class]]) {
                name = (NSString *)n;
            }
        }
        NSMutableDictionary *waterfallResponse = [NSMutableDictionary dictionary];
        if (name != nil && [name length] > 0) {
            [waterfalls addObject: name];
            [waterfallResponse setObject: name forKey: @"name"];
        }
        NSString *loadState = @"FAILED_TO_LOAD";
        if (other.adLoadState == MAAdLoadStateAdLoadNotAttempted) {
            loadState = @"AD_LOAD_NOT_ATTEMPTED";
        } else if (other.adLoadState == MAAdLoadStateAdLoaded) {
            loadState = @"AD_LOADED";
        }
        [waterfallResponse setObject: loadState forKey: @"ad_load_state"];
        [waterfallResponse setObject: @(other.isBidding) forKey: @"is_bidding"];
        [waterfallResponse setObject: @(other.latency) forKey: @"latency_millis"];
        MAError *error = other.error;
        if (error != nil) {
            NSMutableDictionary *jError = [NSMutableDictionary dictionary];
            [jError setObject: @(error.code) forKey: @"code"];
            if (error.message != nil) {
                [jError setObject: error.message forKey: @"name"];
            }
            [waterfallResponse setObject: jError forKey: @"error"];
        }
        [waterfallResponses addObject: waterfallResponse];
    }
    [data setObject: waterfalls forKey: @"waterfall"];
    [data setObject: waterfallResponses forKey: @"waterfall_responses"];
}

- (void)initializeWithParameters:(id<MAAdapterInitializationParameters>)parameters completionHandler:(void (^)(MAAdapterInitializationStatus, NSString *_Nullable))completionHandler {
    if (_plugin != nil) {
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    } else {
        NSString *appId = parameters.serverParameters[@"app_id"];
        
        _plugin = [NeftaPlugin InitWithAppId: appId integration: @"native-applovin-max"];
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
    return @"4.4.4";
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
    
    [_ad Show: [self GetViewController: parameters]];
}

- (void) loadRewardedAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MARewardedAdapterDelegate>)delegate {
    _ad = [[ALNeftaRewarded alloc] initWithId: parameters.thirdPartyAdPlacementIdentifier listener: delegate];
    [self Load: parameters];
}

- (void) showRewardedAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MARewardedAdapterDelegate>)delegate {
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
    [_ad Show: [self GetViewController: parameters]];
}

- (void) Load:(id<MAAdapterResponseParameters>)parameters {
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

- (UIViewController *) GetViewController:(id<MAAdapterResponseParameters>)parameters {
    if (ALSdk.versionCode >= 11020199) {
        return parameters.presentingViewController ?: [ALUtils topViewControllerFromKeyWindow];
    }
    return [ALUtils topViewControllerFromKeyWindow];
}
@end
