//
//  ALNeftaInterstitial.m
//  MaxIntegration
//
//  Created by Tomaz Treven on 3. 10. 24.
//

#import "ALNeftaInterstitial.h"
#import "ALNeftaMediationAdapter.h"

static NSString* _lastCreativeId;
static NSString* _lastAuctionId;

@implementation ALNeftaInterstitial

- (instancetype)initWithId:(NSString *)id listener:(id<MAInterstitialAdapterDelegate>)listener {
    self = [super init];
    if (self) {
        _interstitial = [[NInterstitial alloc] initWithId: id];
        _interstitial._listener = self;
        _listener = listener;
    }
    return self;
}

- (void) SetCustomParameterWithProvider:(NSString *)provider value: (NSString *)value {
    [_interstitial SetCustomParameterWithProvider: provider value: value];
}
- (void) Load {
    [_interstitial Load];
}
- (int) CanShow {
    return (int)[_interstitial CanShow];
}
- (void) Show:(UIViewController *)viewController {
    [_interstitial Show: viewController];
}

- (void)OnLoadFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    MAAdapterError* mError = [ALNeftaAd NLoadToAdapterError: error];
    [_listener didFailToLoadInterstitialAdWithError: mError];
}
- (void)OnLoadWithAd:(NAd * _Nonnull)ad width:(NSInteger)width height:(NSInteger)height {
    [_listener didLoadInterstitialAd];
}
- (void)OnShowFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    [_listener didFailToDisplayInterstitialAdWithError: MAAdapterError.adDisplayFailedError];
}
- (void)OnShowWithAd:(NAd * _Nonnull)ad {
    _lastAuctionId = ad._bid._auctionId;
    _lastCreativeId = ad._bid._creativeId;
    [_listener didDisplayInterstitialAd];
}
- (void)OnClickWithAd:(NAd * _Nonnull)ad {
    [_listener didClickInterstitialAd];
}
- (void)OnCloseWithAd:(NAd * _Nonnull)ad {
    [_listener didHideInterstitialAd];
}

+ (NSString*) GetLastAuctionId {
    return _lastAuctionId;
}
+ (NSString*) GetLastCreativeId {
    return _lastCreativeId;
}
@end
