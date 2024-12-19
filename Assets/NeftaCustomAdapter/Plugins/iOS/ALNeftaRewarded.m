//
//  ALNeftaRewarded.m
//  MaxIntegration
//
//  Created by Tomaz Treven on 3. 10. 24.
//

#import "ALNeftaRewarded.h"

@implementation ALNeftaRewarded
- (instancetype)initWithId:(NSString *)id listener:(id<MARewardedAdapterDelegate>)listener {
    self = [super init];
    if (self) {
        _rewarded = [[NRewarded alloc] initWithId: id];
        _rewarded._listener = self;
        _listener = listener;
    }
    return self;
}

- (void) SetCustomParameterWithProvider:(NSString *)provider value: (NSString *)value {
    [_rewarded SetCustomParameterWithProvider: provider value: value];
}
- (void) Load {
    [_rewarded Load];
}
- (int) CanShow {
    return (int)[_rewarded CanShow];
}
- (void) Show {
    [_rewarded Show];
}

- (void)OnLoadFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    [_listener didFailToLoadRewardedAdWithError: MAAdapterError.unspecified];
}
- (void)OnLoadWithAd:(NAd * _Nonnull)ad width:(NSInteger)width height:(NSInteger)height {
    [_listener didLoadRewardedAd];
}
- (void)OnShowFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    [_listener didFailToLoadRewardedAdWithError: MAAdapterError.adDisplayFailedError];
}
- (void)OnShowWithAd:(NAd * _Nonnull)ad {
    [_listener didDisplayRewardedAd];
}
- (void)OnClickWithAd:(NAd * _Nonnull)ad {
    [_listener didClickRewardedAd];
}
- (void)OnRewardWithAd:(NAd * _Nonnull)ad {
    _giveReward = true;
    if (_reward == nil) {
        _reward = [MAReward rewardWithAmount: MAReward.defaultAmount label: MAReward.defaultLabel];
    }
}
- (void)OnCloseWithAd:(NAd * _Nonnull)ad {
    if (_giveReward) {
        [_listener didRewardUserWithReward: _reward];
    }
    [_listener didHideRewardedAd];
}
@end
