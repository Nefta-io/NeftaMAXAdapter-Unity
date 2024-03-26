//
//  ALNeftaMediationAdapter.m
//  NeftaMaxAdapter
//
//  Created by Tomaz Treven on 09/11/2023.
//

#import "ALNeftaMediationAdapter.h"
#import <AppLovinSDK/MAAdapterDelegate.h>

@interface ALNeftaMediationAdapter ()

@end

@implementation ALNeftaMediationAdapter

static NeftaPlugin_iOS *_plugin;
static NSMutableDictionary<NSString *, id<MAAdapterDelegate>> *_listeners;

NSString* _placementId;

- (void)initializeWithParameters:(id<MAAdapterInitializationParameters>)parameters completionHandler:(void (^)(MAAdapterInitializationStatus, NSString *_Nullable))completionHandler {
    if (_plugin != nil) {
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    } else {
        NSString *appId = parameters.serverParameters[@"app_id"];
        
        [NeftaPlugin_iOS EnableLogging: true];
        _plugin = [NeftaPlugin_iOS InitWithAppId: appId];
        
        _listeners = [[NSMutableDictionary alloc] init];
        
        _plugin.OnLoadFail = ^(Placement *placement, NSString *error) {
            id<MAAdapterDelegate> listener = _listeners[placement._id];
            if (placement._type == TypesBanner) {
                [((id<MAAdViewAdapterDelegate>)listener) didFailToLoadAdViewAdWithError: MAAdapterError.unspecified];
            } else if (placement._type == TypesInterstitial) {
                [((id<MAInterstitialAdapterDelegate>)listener) didFailToLoadInterstitialAdWithError: MAAdapterError.unspecified];
            } else if (placement._type == TypesRewardedVideo) {
                [((id<MARewardedAdapterDelegate>)listener) didFailToLoadRewardedAdWithError: MAAdapterError.unspecified];
            }
            [_listeners removeObjectForKey: placement._id];
        };
        _plugin.OnLoad = ^(Placement *placement) {
            id<MAAdapterDelegate> listener = _listeners[placement._id];
            if (placement._type == TypesBanner) {
                dispatch_async(dispatch_get_main_queue(), ^{
                    placement._isManualPosition = true;
                    UIView *v = [_plugin GetViewForPlacement: placement show: true];
                    [((id<MAAdViewAdapterDelegate>)listener) didLoadAdForAdView: v];
                });
            } else if (placement._type == TypesInterstitial) {
                [((id<MAInterstitialAdapterDelegate>)listener) didLoadInterstitialAd];
            } else if (placement._type == TypesRewardedVideo) {
                [((id<MARewardedAdapterDelegate>)listener) didLoadRewardedAd];
            }
        };
        _plugin.OnShow = ^(Placement *placement, NSInteger width, NSInteger height) {
            id<MAAdapterDelegate> listener = _listeners[placement._id];
            if (placement._type == TypesBanner) {
                [((id<MAAdViewAdapterDelegate>)listener) didDisplayAdViewAd];
            } else if (placement._type == TypesInterstitial) {
                [((id<MAInterstitialAdapterDelegate>)listener) didDisplayInterstitialAd];
            } else if (placement._type == TypesRewardedVideo) {
                [((id<MARewardedAdapterDelegate>)listener) didDisplayRewardedAd];
                [((id<MARewardedAdapterDelegate>)listener) didStartRewardedAdVideo];
            }
        };
        _plugin.OnClick = ^(Placement *placement) {
            id<MAAdapterDelegate> listener = _listeners[placement._id];
            if (placement._type == TypesBanner) {
                [((id<MAAdViewAdapterDelegate>)listener) didClickAdViewAd];
            } else if (placement._type == TypesInterstitial) {
                [((id<MAInterstitialAdapterDelegate>)listener) didClickInterstitialAd];
            } else if (placement._type == TypesRewardedVideo) {
                [((id<MARewardedAdapterDelegate>)listener) didClickRewardedAd];
            }
        };
        _plugin.OnReward = ^(Placement *placement) {
            id<MARewardedAdapterDelegate> listener = (id<MARewardedAdapterDelegate>) _listeners[placement._id];
            if (listener != nil) {
                [listener didCompleteRewardedAdVideo];
            }
        };
        _plugin.OnClose = ^(Placement *placement) {
            id<MAAdapterDelegate> listener = _listeners[placement._id];
            if (placement._type == TypesBanner) {
                [((id<MAAdViewAdapterDelegate>)listener) didCollapseAdViewAd];
                [((id<MAAdViewAdapterDelegate>)listener) didHideAdViewAd];
            } else if (placement._type == TypesInterstitial) {
                [((id<MAInterstitialAdapterDelegate>)listener) didHideInterstitialAd];
            } else if (placement._type == TypesRewardedVideo) {
                [((id<MARewardedAdapterDelegate>)listener) didHideRewardedAd];
            }
            [_listeners removeObjectForKey: placement._id];
        };
        
        [_plugin EnableAds: true];
        
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    }
}

- (NSString *)SDKVersion {
    return NeftaPlugin_iOS.Version;
}

- (NSString *)adapterVersion {
    return @"1.1.6";
}

- (void)destroy {
    if (_placementId != nil) {
        [_plugin CloseWithId: _placementId];
        _placementId = nil;
    }
}

- (void)loadAdViewAdForParameters:(id<MAAdapterResponseParameters>)parameters adFormat:(MAAdFormat *)adFormat andNotify:(id<MAAdViewAdapterDelegate>)delegate {
    NSString *pid = parameters.thirdPartyAdPlacementIdentifier;
    _listeners[pid] = delegate;
    [ALNeftaMediationAdapter ApplyRenderer: parameters];
    
    [_plugin LoadWithId: pid];
}

- (void)loadInterstitialAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MAInterstitialAdapterDelegate>)delegate {
    NSString *pid = parameters.thirdPartyAdPlacementIdentifier;
    _listeners[pid] = delegate;
    [ALNeftaMediationAdapter ApplyRenderer: parameters];
    
    [_plugin LoadWithId: pid];
}

- (void)showInterstitialAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MAInterstitialAdapterDelegate>)delegate {
    _placementId = parameters.thirdPartyAdPlacementIdentifier;
    if (![_plugin IsReadyWithId: _placementId]) {
        [delegate didFailToDisplayInterstitialAdWithError: MAAdapterError.adNotReady];
        return;
    }
    
    [_plugin ShowWithId: _placementId];
}

- (void)loadRewardedAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MARewardedAdapterDelegate>)delegate {
    NSString *pid = parameters.thirdPartyAdPlacementIdentifier;
    [ALNeftaMediationAdapter ApplyRenderer: parameters];
    
    _listeners[pid] = delegate;
    [_plugin LoadWithId: pid];
}

- (void)showRewardedAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MARewardedAdapterDelegate>)delegate {
    _placementId = parameters.thirdPartyAdPlacementIdentifier;
    if (![_plugin IsReadyWithId: _placementId]) {
        [delegate didFailToLoadRewardedAdWithError: MAAdapterError.adNotReady];
        return;
    }

    [_plugin ShowWithId: _placementId];
}

+ (void)ApplyRenderer:(id<MAAdapterResponseParameters>)parameters {
    UIViewController *viewController;
    if (ALSdk.versionCode >= 11020199) {
        viewController = parameters.presentingViewController ?: [ALUtils topViewControllerFromKeyWindow];
    } else {
        viewController = [ALUtils topViewControllerFromKeyWindow];
    }
    [_plugin PrepareRendererWithViewController: viewController];
}
@end
