import { NotFoundError } from './not-found-error';

describe('NotFoundError', () => {
  it('should create an instance', () => {
    expect(new NotFoundError()).toBeTruthy();
  });
});
