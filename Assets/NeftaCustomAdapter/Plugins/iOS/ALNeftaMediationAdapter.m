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
static NSMutableArray *_adapters;
static ALNeftaMediationAdapter *_lastBanner;

- (void)initializeWithParameters:(id<MAAdapterInitializationParameters>)parameters completionHandler:(void (^)(MAAdapterInitializationStatus, NSString *_Nullable))completionHandler {
    if (_plugin != nil) {
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    } else {
        NSString *appId = parameters.serverParameters[@"app_id"];
        
        _plugin = [NeftaPlugin_iOS InitWithAppId: appId];
        
        _adapters = [NSMutableArray array];
        
        _plugin.OnLoadFail = ^(Placement *placement, NSString *error) {
            for (int i = 0; i < _adapters.count; i++) {
                ALNeftaMediationAdapter *a = _adapters[i];
                if ([a.placementId isEqualToString: placement._id] && a.state == 0) {
                    if (placement._type == TypesBanner) {
                        [((id<MAAdViewAdapterDelegate>)a.listener) didFailToLoadAdViewAdWithError: MAAdapterError.unspecified];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>)a.listener) didFailToLoadInterstitialAdWithError: MAAdapterError.unspecified];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>)a.listener) didFailToLoadRewardedAdWithError: MAAdapterError.unspecified];
                    }
                    [_adapters removeObject:a];
                    return;
                }
            }
        };
        _plugin.OnLoad = ^(Placement *placement) {
            for (int i = 0; i < _adapters.count; i++) {
                ALNeftaMediationAdapter *a = _adapters[i];
                if ([a.placementId isEqualToString: placement._id] && a.state == 0) {
                    a.state = 1;
                    ALNeftaMediationAdapter *a = _adapters[i];
                    if (placement._type == TypesBanner) {
                        placement._isManualPosition = true;
                        UIView *v = [_plugin GetViewForPlacement: placement show: true];
                        [((id<MAAdViewAdapterDelegate>)a.listener) didLoadAdForAdView: v];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>)a.listener) didLoadInterstitialAd];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>)a.listener) didLoadRewardedAd];
                    }
                    return;
                }
            }
        };
        _plugin.OnShow = ^(Placement *placement, NSInteger width, NSInteger height) {
            for (int i = 0; i < _adapters.count; i++) {
                ALNeftaMediationAdapter *a = _adapters[i];
                if ([a.placementId isEqualToString: placement._id] && a.state == 1) {
                    a.state = 2;
                    if (placement._type == TypesBanner) {
                        [((id<MAAdViewAdapterDelegate>)a.listener) didDisplayAdViewAd];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>)a.listener) didDisplayInterstitialAd];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>)a.listener) didDisplayRewardedAd];
                    }
                    return;
                }
            }
        };
        _plugin.OnClick = ^(Placement *placement) {
            for (int i = 0; i < _adapters.count; i++) {
                ALNeftaMediationAdapter *a = _adapters[i];
                if ([a.placementId isEqualToString: placement._id] && a.state == 2) {
                    if (placement._type == TypesBanner) {
                        [((id<MAAdViewAdapterDelegate>) a.listener) didClickAdViewAd];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>) a.listener) didClickInterstitialAd];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>) a.listener) didClickRewardedAd];
                    }
                    return;
                }
            }
        };
        _plugin.OnReward = ^(Placement *placement) {
            for (int i = 0; i < _adapters.count; i++) {
                ALNeftaMediationAdapter *a = _adapters[i];
                if ([a.placementId isEqualToString: placement._id] && a.state == 2) {
                    id<MARewardedAdapterDelegate> listener = (id<MARewardedAdapterDelegate>)a.listener;
                    MAReward *reward = [MAReward rewardWithAmount:MAReward.defaultAmount label: MAReward.defaultLabel];
                    [listener didRewardUserWithReward: reward];
                    return;
                }
            }
        };
        _plugin.OnClose = ^(Placement *placement) {
            for (int i = 0; i < _adapters.count; i++) {
                ALNeftaMediationAdapter *a = _adapters[i];
                if ([a.placementId isEqualToString: placement._id] && a.state == 2) {
                    if (placement._type == TypesBanner) {
                        id<MAAdViewAdapterDelegate> bannerListener = (id<MAAdViewAdapterDelegate>)a.listener;
                        [bannerListener didCollapseAdViewAd];
                        [bannerListener didHideAdViewAd];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>)a.listener) didHideInterstitialAd];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>)a.listener) didHideRewardedAd];
                    }
                    [_adapters removeObject: a];
                    return;
                }
            }
        };
        
        [_plugin EnableAds: true];
        
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    }
}

- (NSString *)SDKVersion {
    return NeftaPlugin_iOS.Version;
}

- (NSString *)adapterVersion {
    return @"1.2.0";
}

- (void)destroy {
    bool isLastBanner = _lastBanner == self;
    if (_bL == nil || isLastBanner) {
        [_plugin CloseWithId: _placementId];
        if (isLastBanner) {
            _lastBanner = nil;
        }
    } else {
        [_bL didCollapseAdViewAd];
        [_bL didHideAdViewAd];
    }
}

- (void)loadAdViewAdForParameters:(id<MAAdapterResponseParameters>)parameters adFormat:(MAAdFormat *)adFormat andNotify:(id<MAAdViewAdapterDelegate>)delegate {
    _placementId = parameters.thirdPartyAdPlacementIdentifier;
    _state = 0;
    _listener = delegate;
    
    [ALNeftaMediationAdapter ApplyRenderer: parameters];

    _lastBanner = self;

    [_adapters addObject: self];
    [_plugin LoadWithId: _placementId];
}

- (void)loadInterstitialAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MAInterstitialAdapterDelegate>)delegate {
    _placementId = parameters.thirdPartyAdPlacementIdentifier;
    _state = 0;
    _listener = delegate;
    
    [ALNeftaMediationAdapter ApplyRenderer: parameters];
    
    [_adapters addObject: self];
    [_plugin LoadWithId: _placementId];
}

- (void)showInterstitialAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MAInterstitialAdapterDelegate>)delegate {
    if (![_plugin IsReadyWithId: _placementId]) {
        [delegate didFailToDisplayInterstitialAdWithError: MAAdapterError.adNotReady];
        return;
    }
    
    [_plugin ShowWithId: _placementId];
}

- (void)loadRewardedAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MARewardedAdapterDelegate>)delegate {
    _placementId = parameters.thirdPartyAdPlacementIdentifier;
    _state = 0;
    _listener = delegate;
    
    [ALNeftaMediationAdapter ApplyRenderer: parameters];
    
    [_adapters addObject: self];
    [_plugin LoadWithId: _placementId];
}

- (void)showRewardedAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MARewardedAdapterDelegate>)delegate {
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
