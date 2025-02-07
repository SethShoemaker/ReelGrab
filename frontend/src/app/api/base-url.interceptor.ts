import { HttpInterceptorFn } from '@angular/common/http';

export const baseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const newReq = req.clone({ url: `${'http://localhost:5242'}${req.url}` })
  return next(newReq);
};
