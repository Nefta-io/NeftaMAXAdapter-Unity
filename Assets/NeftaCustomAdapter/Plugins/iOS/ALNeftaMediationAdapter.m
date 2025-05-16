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

#import <AppLovinSDK/AppLovinSDK.h>

NSString * const _mediationProvider = @"applovin-max";

@implementation ALNeftaMediationAdapter

static NeftaPlugin *_plugin;

+ (void)OnExternalMediationRequestLoad:(AdType)adType recommendedAdUnitId:(NSString* _Nullable)recommendedAdUnitId calculatedFloorPrice:(double)calculatedFloorPrice ad:(MAAd * _Nonnull)ad {
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: adType recommendedAdUnitId: recommendedAdUnitId requestedFloorPrice: -1 calculatedFloorPrice: calculatedFloorPrice adUnitId: ad.adUnitIdentifier revenue: ad.revenue precision: ad.revenuePrecision status: 1];
}
+ (void)OnExternalMediationRequestFail:(AdType)adType recommendedAdUnitId:(NSString* _Nullable)recommendedAdUnitId calculatedFloorPrice:(double)calculatedFloorPrice adUnitIdentifier:(NSString * _Nonnull)adUnitIdentifier error:(MAError * _Nonnull)error {
    int status = 0;
    if (error.code == MAErrorCodeNoFill) {
        status = 2;
    }
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: adType recommendedAdUnitId: recommendedAdUnitId requestedFloorPrice: -1 calculatedFloorPrice: calculatedFloorPrice adUnitId: adUnitIdentifier revenue: -1 precision: nil status: status];
}

+ (void) OnExternalMediationRequestLoad:(AdType)adType requestedFloorPrice:(double)requestedFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice ad:(MAAd *)ad {
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: adType recommendedAdUnitId: nil requestedFloorPrice: requestedFloorPrice calculatedFloorPrice: calculatedFloorPrice adUnitId: ad.adUnitIdentifier revenue: ad.revenue precision: ad.revenuePrecision status: 1];
}

+ (void) OnExternalMediationRequestFail:(AdType)adType requestedFloorPrice:(double)requestedFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice adUnitIdentifier:(NSString *)adUnitIdentifier error:(MAError *)error {
    int status = 0;
    if (error.code == MAErrorCodeNoFill) {
        status = 2;
    }
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: adType recommendedAdUnitId: nil requestedFloorPrice: requestedFloorPrice calculatedFloorPrice: calculatedFloorPrice adUnitId: adUnitIdentifier revenue: -1 precision: nil status: status];
}

+ (void) OnExternalMediationImpression:(MAAd*)ad {
    NSMutableDictionary *data = [NSMutableDictionary dictionary];
    [data setObject: _mediationProvider forKey: @"mediation_provider"];
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
    NSString* auctionId;
    NSString* creativeId = ad.creativeIdentifier;
    if ([ad.networkName isEqualToString: @"Nefta"]) {
        if (ad.format == MAAdFormat.banner || ad.format == MAAdFormat.leader) {
            auctionId = ALNeftaBanner.GetLastAuctionId;
            creativeId = ALNeftaBanner.GetLastCreativeId;
        } else if (ad.format == MAAdFormat.interstitial) {
            auctionId = ALNeftaInterstitial.GetLastAuctionId;
            creativeId = ALNeftaInterstitial.GetLastCreativeId;
        } else if (ad.format == MAAdFormat.rewarded) {
            auctionId = ALNeftaRewarded.GetLastAuctionId;
            creativeId = ALNeftaRewarded.GetLastCreativeId;
        }
    }
    if (auctionId != nil) {
        [data setObject: auctionId forKey: @"ad_opportunity_id"];
    }
    if (creativeId != nil) {
        [data setObject: creativeId forKey: @"creative_id"];
    }
    [NeftaPlugin OnExternalMediationImpression: _mediationProvider data: data];
}

+ (void) OnExternalMediationImpressionAsString:(NSString*)network format:(NSString *)format creativeId:(NSString *)creativeId data:(NSString *)data {
    NSString *auctionId = nil;
    if ([network isEqual: @"Nefta"]) {
        if ([format isEqualToString: MAAdFormat.banner.label] || [format isEqualToString: MAAdFormat.leader.label]) {
            auctionId = ALNeftaBanner.GetLastAuctionId;
            creativeId = ALNeftaBanner.GetLastCreativeId;
        } else if ([format isEqualToString: MAAdFormat.interstitial.label]) {
            auctionId = ALNeftaInterstitial.GetLastAuctionId;
            creativeId = ALNeftaInterstitial.GetLastCreativeId;
        } else if ([format isEqualToString: MAAdFormat.rewarded.label]) {
            auctionId = ALNeftaRewarded.GetLastAuctionId;
            creativeId = ALNeftaRewarded.GetLastCreativeId;
        }
    }
    
    NSMutableString *sb = [[NSMutableString alloc] initWithString: data];
    [sb appendString: @",\"network_name\":\""];
    [sb appendString: network];
    [sb appendString: @"\",\"format\":\""];
    [sb appendString: format];
    if (auctionId != nil) {
        [sb appendString: @"\",\"ad_opportunity_id\":\""];
        [sb appendString: auctionId];
    }
    if (creativeId != nil) {
        [sb appendString: @"\",\"creative_id\":\""];
        [sb appendString: creativeId];
    }
    [sb appendString: @"\""];
    
    [NeftaPlugin OnExternalMediationImpressionAsString: _mediationProvider data: sb];
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
    return @"2.2.3";
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
