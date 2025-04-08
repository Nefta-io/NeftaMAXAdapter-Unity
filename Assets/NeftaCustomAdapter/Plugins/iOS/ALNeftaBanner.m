//
//  ALNeftaBanner.m
//  MaxIntegration
//
//  Created by Tomaz Treven on 3. 10. 24.
//

#import "ALNeftaBanner.h"

static NSString* _lastCreativeId;
static NSString* _lastAuctionId;

@implementation ALNeftaBanner
- (instancetype)initWithId:(NSString *)id listener:(id<MAAdViewAdapterDelegate>)listener {
    self = [super init];
    if (self) {
        _banner = [[NBanner alloc] initWithId: id position: PositionNone];
        _banner._listener = self;
        _listener = listener;
    }
    return self;
}

- (void) SetCustomParameterWithProvider:(NSString *)provider value: (NSString *)value {
    [_banner SetCustomParameterWithProvider: provider value: value];
}
- (void) Load {
    [_banner Load];
}
- (void) Close {
    [_banner Close];
}

- (void)OnLoadFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    MAAdapterError* mError = [ALNeftaAd NLoadToAdapterError: error];
    [_listener didFailToLoadAdViewAdWithError: mError];
}
- (void)OnLoadWithAd:(NAd * _Nonnull)ad width:(NSInteger)width height:(NSInteger)height {
    UIView *v = [_banner GracefulShow: nil];
    [_listener didLoadAdForAdView: v];
}
- (void)OnShowFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    [_listener didFailToDisplayAdViewAdWithError: MAAdapterError.adDisplayFailedError];
}
- (void)OnShowWithAd:(NAd * _Nonnull)ad {
    _lastAuctionId = ad._bid._auctionId;
    _lastCreativeId = ad._bid._creativeId;
    [_listener didDisplayAdViewAd];
}
- (void)OnClickWithAd:(NAd * _Nonnull)ad {
    [_listener didClickAdViewAd];
}
- (void)OnCloseWithAd:(NAd * _Nonnull)ad {
    [_listener didCollapseAdViewAd];
    [_listener didHideAdViewAd];
}

+ (NSString*) GetLastAuctionId {
    return _lastAuctionId;
}
+ (NSString*) GetLastCreativeId {
    return _lastCreativeId;
}
@end
