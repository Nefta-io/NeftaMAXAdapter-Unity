//
//  ALNeftaAd.h
//  MaxAdapter
//
//  Created by Tomaz Treven on 3. 10. 24.
//

#import <AppLovinSDK/AppLovinSDK.h>
#import <NeftaSDK/NeftaSDK-Swift.h>

@interface ALNeftaAd : NSObject
- (void) SetCustomParameterWithProvider:(NSString *_Nonnull)provider value: (NSString *_Nonnull)value;
- (void) Load;
- (int) CanShow;
- (void) Show;
- (void) Close;
@end
