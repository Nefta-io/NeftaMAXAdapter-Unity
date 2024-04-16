//
//  ALNeftaMediationAdapter.m
//  NeftaMaxAdapter
//
//  Created by Tomaz Treven on 09/11/2023.
//

#import "ALNeftaMediationAdapter.h"
#import <AppLovinSDK/MAAdapterDelegate.h>

@interface MListener : NSObject
@property (nonatomic, strong) NSString* placementId;
@property (nonatomic) int state;
@property (nonatomic, strong) id<MAAdapterDelegate> listener;
-(instancetype)initWithId:(NSString *)placementId listener:(id<MAAdapterDelegate>)listener;
@end
@implementation MListener
-(instancetype)initWithId:(NSString *)placementId listener:(id<MAAdapterDelegate>)listener {
    self = [super init];
    if (self) {
        _placementId = placementId;
        _state = 0;
        _listener = listener;
    }
    return self;
}
@end

@interface ALNeftaMediationAdapter ()

@end
@implementation ALNeftaMediationAdapter

static NeftaPlugin_iOS *_plugin;
static NSMutableArray *_listeners;
static ALNeftaMediationAdapter *_lastBanner;

id<MAAdViewAdapterDelegate> _bL;
NSString* _placementId;

- (void)initializeWithParameters:(id<MAAdapterInitializationParameters>)parameters completionHandler:(void (^)(MAAdapterInitializationStatus, NSString *_Nullable))completionHandler {
    if (_plugin != nil) {
        completionHandler(MAAdapterInitializationStatusInitializedSuccess, nil);
    } else {
        NSString *appId = parameters.serverParameters[@"app_id"];
        
        _plugin = [NeftaPlugin_iOS InitWithAppId: appId];
        
        _listeners = [NSMutableArray array];
        
        _plugin.OnLoadFail = ^(Placement *placement, NSString *error) {
            for (int i = 0; i < _listeners.count; i++) {
                MListener *ml = _listeners[i];
                if ([ml.placementId isEqualToString: placement._id] && ml.state == 0) {
                    if (placement._type == TypesBanner) {
                        [((id<MAAdViewAdapterDelegate>)ml.listener) didFailToLoadAdViewAdWithError: MAAdapterError.unspecified];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>)ml.listener) didFailToLoadInterstitialAdWithError: MAAdapterError.unspecified];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>)ml.listener) didFailToLoadRewardedAdWithError: MAAdapterError.unspecified];
                    }
                    [_listeners removeObject:ml];
                    return;
                }
            }
        };
        _plugin.OnLoad = ^(Placement *placement) {
            for (int i = 0; i < _listeners.count; i++) {
                MListener *ml = _listeners[i];
                if ([ml.placementId isEqualToString: placement._id] && ml.state == 0) {
                    ml.state = 1;
                    MListener *ml = _listeners[i];
                    if (placement._type == TypesBanner) {
                        placement._isManualPosition = true;
                        UIView *v = [_plugin GetViewForPlacement: placement show: true];
                        [((id<MAAdViewAdapterDelegate>)ml.listener) didLoadAdForAdView: v];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>)ml.listener) didLoadInterstitialAd];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>)ml.listener) didLoadRewardedAd];
                    }
                    return;
                }
            }
        };
        _plugin.OnShow = ^(Placement *placement, NSInteger width, NSInteger height) {
            for (int i = 0; i < _listeners.count; i++) {
                MListener *ml = _listeners[i];
                if ([ml.placementId isEqualToString: placement._id] && ml.state == 1) {
                    ml.state = 2;
                    if (placement._type == TypesBanner) {
                        [((id<MAAdViewAdapterDelegate>)ml.listener) didDisplayAdViewAd];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>)ml.listener) didDisplayInterstitialAd];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>)ml.listener) didDisplayRewardedAd];
                        [((id<MARewardedAdapterDelegate>)ml.listener) didStartRewardedAdVideo];
                    }
                    return;
                }
            }
        };
        _plugin.OnClick = ^(Placement *placement) {
            for (int i = 0; i < _listeners.count; i++) {
                MListener *ml = _listeners[i];
                if ([ml.placementId isEqualToString: placement._id] && ml.state == 2) {
                    if (placement._type == TypesBanner) {
                        [((id<MAAdViewAdapterDelegate>) ml.listener) didClickAdViewAd];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>) ml.listener) didClickInterstitialAd];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>) ml.listener) didClickRewardedAd];
                    }
                    return;
                }
            }
        };
        _plugin.OnReward = ^(Placement *placement) {
            for (int i = 0; i < _listeners.count; i++) {
                MListener *ml = _listeners[i];
                if ([ml.placementId isEqualToString: placement._id] && ml.state == 2) {
                    id<MARewardedAdapterDelegate> listener = (id<MARewardedAdapterDelegate>)ml.listener;
                    [listener didCompleteRewardedAdVideo];
                    return;
                }
            }
        };
        _plugin.OnClose = ^(Placement *placement) {
            for (int i = 0; i < _listeners.count; i++) {
                MListener *ml = _listeners[i];
                if ([ml.placementId isEqualToString: placement._id] && ml.state == 2) {
                    if (placement._type == TypesBanner) {
                        id<MAAdViewAdapterDelegate> bannerListener = (id<MAAdViewAdapterDelegate>)ml.listener;
                        [bannerListener didCollapseAdViewAd];
                        [bannerListener didHideAdViewAd];
                    } else if (placement._type == TypesInterstitial) {
                        [((id<MAInterstitialAdapterDelegate>)ml.listener) didHideInterstitialAd];
                    } else if (placement._type == TypesRewardedVideo) {
                        [((id<MARewardedAdapterDelegate>)ml.listener) didHideRewardedAd];
                    }
                    [_listeners removeObject: ml];
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
    return @"1.1.8";
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
    [ALNeftaMediationAdapter ApplyRenderer: parameters];
    
    _bL = delegate;
    _lastBanner = self;
    MListener *listener = [[MListener alloc] initWithId: _placementId listener: delegate];
    [_listeners addObject: listener];
    [_plugin LoadWithId: _placementId];
}

- (void)loadInterstitialAdForParameters:(id<MAAdapterResponseParameters>)parameters andNotify:(id<MAInterstitialAdapterDelegate>)delegate {
    _placementId = parameters.thirdPartyAdPlacementIdentifier;
    [ALNeftaMediationAdapter ApplyRenderer: parameters];
    
    MListener *listener = [[MListener alloc] initWithId: _placementId listener: delegate];
    [_listeners addObject: listener];
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
    [ALNeftaMediationAdapter ApplyRenderer: parameters];
    
    MListener *listener = [[MListener alloc] initWithId: _placementId listener: delegate];
    [_listeners addObject: listener];
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
